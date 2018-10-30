using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;

namespace church.ccv.Utility.Jobs
{
    /// <summary>
    /// Sends notifications to group leaders when a group member has a follow up date
    /// </summary>
    [SystemEmailField( "Email Template", "The email template used for the notification.", required: true )]
    [GroupTypesField("Group Types", "Group types to include.",true)]
    [DisallowConcurrentExecution]
    class GroupMemberFollowUpNotifications : IJob
    {
        /// <summary> 
        /// Empty constructor for job initialization
        /// <para>
        /// Jobs require a public empty constructor so that the
        /// scheduler can instantiate the class whenever it needs.
        /// </para>
        /// </summary>
        public GroupMemberFollowUpNotifications()
        {
        }

        /// <summary>
        /// Executes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public void Execute(IJobExecutionContext context)
        {
            var rockContext = new RockContext();

            SystemEmailService emailService = new SystemEmailService( rockContext );
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );

            JobDataMap dataMap = context.JobDetail.JobDataMap;

            Guid? systemEmailGuid = dataMap.GetString( "EmailTemplate" ).AsGuidOrNull();
            List<Guid> groupTypeGuids = dataMap.GetString( "GroupTypes" ).SplitDelimitedValues().AsGuidList();

            // get system email
            SystemEmail systemEmail = null;
            if ( systemEmailGuid.HasValue )
            {
                systemEmail = emailService.Get( systemEmailGuid.Value );
            }

            if ( groupTypeGuids.Any() )
            {
                var groupTypes = new GroupTypeService( rockContext ).GetByGuids( groupTypeGuids );

                var groupMemberEntityTypeId = Rock.Web.Cache.EntityTypeCache.GetId( typeof( GroupMember ) );

                var followUpDateGroupMemberIds = new AttributeValueService( rockContext ).Queryable()
                    .Where( a => a.Attribute.Key == "FollowUpDate" &&
                            a.Attribute.EntityTypeId == groupMemberEntityTypeId &&
                            a.ValueAsDateTime <= RockDateTime.Now )
                    .Select( a => a.EntityId );

                var groupMembers = new GroupMemberService( rockContext ).Queryable()
                    .Where( g =>
                            groupTypes.Contains( g.Group.GroupType ) &&
                            g.GroupMemberStatus == GroupMemberStatus.Inactive &&
                            followUpDateGroupMemberIds.Contains( g.Id ) );

                foreach ( var groupMember in groupMembers )
                {
                    groupMember.LoadAttributes();

                    // set status to pending
                    groupMember.GroupMemberStatus = GroupMemberStatus.Pending;

                    // remove follow up date
                    groupMember.SetAttributeValue( "FollowUpDate", null );

                    // remove opt out reason
                    groupMember.SetAttributeValue( "OptOutReason", null );

                    groupMember.SaveAttributeValues( new RockContext() );

                    // get coach
                    var coach = new GroupMemberService( rockContext )
                        .GetLeaders( groupMember.Group.Id )
                        .Select( m => m.Person )
                        .FirstOrDefault();

                    // send email
                    if ( coach != null )
                    {
                        var mergeFields = new Dictionary<string, object>();
                        mergeFields.Add( "Person", groupMember.Person );
                        mergeFields.Add( "Group", groupMember.Group );
                        mergeFields.Add( "GroupMember", groupMember );

                        var emailMessage = new RockEmailMessage();
                        emailMessage.AddRecipient( new RecipientData( coach.Email, mergeFields ) );
                        emailMessage.AppRoot = Rock.Web.Cache.GlobalAttributesCache.Read( rockContext ).GetValue( "ExternalApplicationRoot" );
                        emailMessage.FromEmail = systemEmail.FromName;
                        emailMessage.FromName = systemEmail.From;
                        emailMessage.Subject = systemEmail.Subject;
                        emailMessage.Message = systemEmail.Body.ResolveMergeFields( mergeFields );
                        emailMessage.CreateCommunicationRecord = true;
                        emailMessage.Send();
                    }
                }

                rockContext.SaveChanges();
            }
        }
    }
}
