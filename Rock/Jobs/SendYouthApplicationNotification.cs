// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Rock.Jobs
{
    /// <summary>
    /// Sends out reminders to group leaders when group members do not meet all requirements.
    /// </summary>
    [SystemEmailField( "Notification Email Template", required: true, order: 0 )]
    [GroupTypesField( "Group Types", "Group types use to check the group requirements on.", order: 1 )]
    [DataViewField("Data View","Which Dataview to use",true, "4A9E2456-04BB-4377-972F-9951876C76E2", "Rock.Model.Person","",2)]
    [EnumField( "Notify Parent Leaders", "", typeof( NotificationOption ), true, "None", order: 3 )]
    [GroupField( "Accountability Group", "Optional group that will receive a list of all group members that are approaching the 7th grade.", false, order: 4 )]
    [DisallowConcurrentExecution]
    public class SendYouthApplicationNotification : IJob
    {
        List<NotificationEntity> _notificationList = new List<NotificationEntity>();
        List<GroupWithYouthMembers> _groupsWithYouthMembers = new List<GroupWithYouthMembers>();

        /// <summary> 
        /// Empty constructor for job initialization
        /// <para>
        /// Jobs require a public empty constructor so that the
        /// scheduler can instantiate the class whenever it needs.
        /// </para>
        /// </summary>
        public SendYouthApplicationNotification()
        {
        }

        /// <summary>
        /// Executes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public void Execute( IJobExecutionContext context )
        {
            var rockContext = new RockContext();

            JobDataMap dataMap = context.JobDetail.JobDataMap;
            Guid? systemEmailGuid = dataMap.GetString( "NotificationEmailTemplate" ).AsGuidOrNull();

            if ( systemEmailGuid.HasValue )
            {
                var selectedGroupTypes = new List<Guid>();
                if ( !string.IsNullOrWhiteSpace( dataMap.GetString( "GroupTypes" ) ) )
                {
                    selectedGroupTypes = dataMap.GetString( "GroupTypes" ).Split( ',' ).Select( Guid.Parse ).ToList();
                }

                var notificationOption = dataMap.GetString( "NotifyParentLeaders" ).ConvertToEnum<NotificationOption>( NotificationOption.None );

                var accountAbilityGroupGuid = dataMap.GetString( "AccountabilityGroup" ).AsGuid();

                var dataViewGuid = dataMap.GetString( "DataView" ).AsGuid();

                var dataViewService = new DataViewService( rockContext );

                var dataView = dataViewService.Get( dataViewGuid );

                var personService = new PersonService( rockContext );

                var personParameterExpression = personService.ParameterExpression;

                List<string> errorMessages;
                
                var dvQry = dataView.GetQuery( null, rockContext, null, 120, out errorMessages );

                IEnumerable<GroupMember> gmList = new List<GroupMember>();

                // get groups matching of the types provided
                GroupService groupService = new GroupService( rockContext );
                var groups = groupService.Queryable().AsNoTracking()
                                .Where( g => selectedGroupTypes.Contains( g.GroupType.Guid )
                                    && g.IsActive == true
                                    && g.Members.Where( gm => dvQry.Any( dvp => dvp.Id == gm.PersonId) ).Any() );

                var groupMemberServiceQry = new GroupMemberService( rockContext ).Queryable();

                List<object> notificationEntities = new List<object>();
               
                foreach(Group group in groups)
                {
                    var parentIds = groupService.GetAllAncestorIds( group.Id );
                    GroupWithYouthMembers groupWithYouthMembers = new GroupWithYouthMembers();
                    groupWithYouthMembers.Id = group.Id;
                    groupWithYouthMembers.Name = group.Name;
                    groupWithYouthMembers.AncestorPathName = groupService.GroupAncestorPathName( group.Id );

                    if ( group.GroupType != null )
                    {
                        groupWithYouthMembers.GroupTypeId = group.GroupTypeId;
                        groupWithYouthMembers.GroupTypeName = group.GroupType.Name;
                    }

                    // get list of the group leaders
                    groupWithYouthMembers.Leaders = group.Members
                        .Where( m => m.GroupRole.IsLeader )
                        .Select( m => new ServingGroupMemberResult
                        {
                            Id = m.Id,
                            PersonId = m.PersonId,
                            FullName = m.Person.FullName
                        } )
                        .ToList();

                    List<GroupYouthMember> groupMembers = new List<GroupYouthMember>();
                    var groupYouthMemberQry = group.Members.Where( gm => dvQry.Any( dvp => dvp.Id == gm.PersonId ) );

                    foreach (var youthMember in groupYouthMemberQry )
                    {
                        GroupYouthMember groupYouthMember = new GroupYouthMember();
                        groupYouthMember.FullName = youthMember.Person.FullName;
                        groupYouthMember.Id = youthMember.Id;
                        groupYouthMember.PersonId = youthMember.PersonId;
                        groupYouthMember.GroupMemberRole = youthMember.GroupRole.Name;

                        groupMembers.Add( groupYouthMember );
                    }

                    groupWithYouthMembers.GroupYouthMembers = groupMembers;

                    _groupsWithYouthMembers.Add( groupWithYouthMembers );

                    foreach (var leader in group.Members.Where( gm => gm.GroupRole.IsLeader ) )
                    {
                        var notificationEntity = new NotificationEntity();
                        notificationEntity.Person = leader.Person;
                        notificationEntity.GroupId = group.Id;
                        _notificationList.Add( notificationEntity );
                    }

                    var parentLeaders = groupMemberServiceQry.Where( lm => parentIds.Contains( lm.GroupId ) && lm.GroupRole.IsLeader );

                    foreach(var parentLeader in parentLeaders )
                    {
                        var notificationEntity = new NotificationEntity();
                        notificationEntity.Person = parentLeader.Person;
                        notificationEntity.GroupId = group.Id;
                        _notificationList.Add( notificationEntity );
                    }
                   
                }

                // send out notificatons
                int recipients = 0;
                var notificationRecipients = _notificationList.GroupBy( p => p.Person.Id ).ToList();
                foreach ( var recipientId in notificationRecipients )
                {
                    var recipient = _notificationList.Where( n => n.Person.Id == recipientId.Key ).Select( n => n.Person ).FirstOrDefault();

                    var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null );
                    mergeFields.Add( "Person", recipient );
                    var notificationGroupIds = _notificationList
                                                    .Where( n => n.Person.Id == recipient.Id )
                                                    .Select( n => n.GroupId )
                                                    .ToList();
                    var withYouthMembers = _groupsWithYouthMembers.Where( g => notificationGroupIds.Contains( g.Id ) ).ToList();
                    mergeFields.Add( "GroupsWithYouthMembers", withYouthMembers );

                    var emailMessage = new RockEmailMessage( systemEmailGuid.Value );
                    emailMessage.AddRecipient( new RecipientData( recipient.Email, mergeFields ) );
                    emailMessage.Send();

                    recipients++;
                }

                context.Result = string.Format( "{0} requirement notification {1} sent", recipients, "email".PluralizeIf( recipients != 1 ) );

            }
            else
            {
                context.Result = "Warning: No NotificationEmailTemplate found";
            }
        }
    }

    #region Helper Classes

    public class NotificationEntity
    {
        /// <summary>
        /// Gets or sets the person identifier.
        /// </summary>
        /// <value>
        /// The person identifier.
        /// </value>
        public Person Person { get; set; }

        /// <summary>
        /// Gets or sets the group identifier.
        /// </summary>
        /// <value>
        /// The group identifier.
        /// </value>
        public int GroupId { get; set; }

    }

    /// <summary>
    /// Group Missing Requirements
    /// </summary>
    [DotLiquid.LiquidType( "Id", "Name", "GroupYouthMembers", "AncestorPathName", "GroupTypeId", "GroupTypeName", "Leaders" )]
    public class GroupWithYouthMembers
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the group members missing requirements.
        /// </summary>
        /// <value>
        /// The group members missing requirements.
        /// </value>
        public List<GroupYouthMember> GroupYouthMembers { get; set; }

        /// <summary>
        /// Gets or sets the name of the ancestor path.
        /// </summary>
        /// <value>
        /// The name of the ancestor path.
        /// </value>
        public string AncestorPathName { get; set; }

        /// <summary>
        /// Gets or sets the group type identifier.
        /// </summary>
        /// <value>
        /// The group type identifier.
        /// </value>
        public int GroupTypeId { get; set; }

        /// <summary>
        /// Gets or sets the name of the group type.
        /// </summary>
        /// <value>
        /// The name of the group type.
        /// </value>
        public string GroupTypeName { get; set; }

        /// <summary>
        /// Gets or sets the leaders.
        /// </summary>
        /// <value>
        /// The leaders.
        /// </value>
        public List<ServingGroupMemberResult> Leaders { get; set; }
    }

    /// <summary>
    /// Group Member Missing Requirements
    /// </summary>
    [DotLiquid.LiquidType( "Id", "PersonId", "FullName", "GroupMemberRole", "MissingRequirements" )]
    public class GroupYouthMember : GroupMemberResult
    {
       
    }

    /// <summary>
    /// Group Member Result
    /// </summary>
    [DotLiquid.LiquidType( "Id", "PersonId", "FullName", "GroupMemberRole" )]
    public class ServingGroupMemberResult
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the person identifier.
        /// </summary>
        /// <value>
        /// The person identifier.
        /// </value>
        public int PersonId { get; set; }

        /// <summary>
        /// Gets or sets the full name.
        /// </summary>
        /// <value>
        /// The full name.
        /// </value>
        public string FullName { get; set; }

        /// <summary>
        /// Gets or sets the group member role.
        /// </summary>
        /// <value>
        /// The group member role.
        /// </value>
        public string GroupMemberRole { get; set; }
    }
    #endregion
}
