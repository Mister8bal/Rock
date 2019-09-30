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
    [GroupField( "Accountability Group", "Optional group that will receive a list of all group members that do not meet requirements.", false, order: 4 )]
    [DisallowConcurrentExecution]
    public class SendYouthApplicationNotification : IJob
    {
        List<NotificationItem> _notificationList = new List<NotificationItem>();
        List<GroupsMissingRequirements> _groupsMissingRequriements = new List<GroupsMissingRequirements>();

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
                //var dvQry = dataView.GetExpression( personService, null, out errorMessages );
                var dvQry = dataView.GetQuery( null, rockContext, null, 120, out errorMessages );

                IEnumerable<GroupMember> gmList = new List<GroupMember>();

                // get groups matching of the types provided
                GroupService groupService = new GroupService( rockContext );
                var groups = groupService.Queryable().AsNoTracking()
                                .Where( g => selectedGroupTypes.Contains( g.GroupType.Guid )
                                    && g.IsActive == true).ToList();

                var groupMemberServiceQry = new GroupMemberService( rockContext ).Queryable();

               foreach(Group group in groups)
                {
                    gmList = gmList.Concat( groupMemberServiceQry.Where( gm => gm.GroupId == group.Id && dvQry.Any( p => p.Id == gm.PersonId ) ).ToList() );
                }
               
            }

            context.Result = "Warning: No NotificationEmailTemplate found";
        }
    }

    #region Helper Classes

    

    #endregion
}
