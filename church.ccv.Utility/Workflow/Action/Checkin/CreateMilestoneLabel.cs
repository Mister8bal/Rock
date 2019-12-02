
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.Entity;
using System.Linq;

using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Rock.Workflow.Action.CheckIn
{
    /// <summary>
    /// Prints out a label if a person met a milestone.
    /// </summary>
    [ActionCategory( "CCV > Check-In" )]
    [Description( "Create a special label to be printed whenever a person meets a check-in milestone." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Create Milestone Label" )]

    [DefinedTypeField( "Check-in Milestone Defined Type", "The defined type that defines the check-in milestones.", true, "", "", 0 )]
    [GroupTypesField( "Group Types Allowed", "Which check-in group types should count towards a milestone.", true, "", "", 1 )]
    public class CreateMilestoneLabel : CheckInActionComponent
    {
        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The workflow action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override bool Execute( RockContext rockContext, Model.WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            var checkInState = GetCheckInState( entity, out errorMessages );
            if ( checkInState != null )
            {
                List<Guid> groupTypeGuids = GetAttributeValue( action, "GroupTypesAllowed" ).SplitDelimitedValues().AsGuidList();
                DefinedTypeCache milestoneDefinedType = DefinedTypeCache.Read( GetAttributeValue( action, "Check-inMilestoneDefinedType" ).AsGuid() );

                // If settings were not configured, skip this action.
                if ( !groupTypeGuids.Any() || milestoneDefinedType == null )
                {
                    return true;
                }

                var attendanceService = new AttendanceService( rockContext );
                List<int> groupTypeIds = new GroupTypeService( rockContext ).GetByGuids( groupTypeGuids ).Select( g => g.Id ).ToList();
                DateTime beginningOfYear = new DateTime( RockDateTime.Today.Year, 1, 1);

                foreach ( var family in checkInState.CheckIn.GetFamilies( true ) )
                {
                    foreach ( var person in family.GetPeople( true ) )
                    {
                        foreach ( var groupType in person.GetGroupTypes( true ) )
                        {
                            // Check to see if the person is checking into one of the group types that count towards milestones. 
                            if ( !groupTypeIds.Contains( groupType.GroupType.Id ) )
                            {
                                return true;
                            }

                            var weekendsAttendedCurrentYear = attendanceService
                                .Queryable().AsNoTracking()
                                .Where( a =>
                                    a.PersonAlias != null &&
                                    a.Group != null &&
                                    a.Schedule != null &&
                                    a.PersonAlias.PersonId == person.Person.Id &&
                                    groupTypeIds.Contains( a.Group.GroupTypeId ) &&
                                    a.StartDateTime >= beginningOfYear &&
                                    a.DidAttend.HasValue &&
                                    a.DidAttend.Value == true )
                                .GroupBy( a => a.SundayDate )
                                .Count();

                            // Check to see if the person's weekends attended matches a milestone.
                            var milestone = milestoneDefinedType.DefinedValues.Where( dv => dv.Value == weekendsAttendedCurrentYear.ToString() ).FirstOrDefault();

                            if ( milestone != null )
                            {
                                string message = milestone.GetAttributeValue( "Message" );
                                Guid? labelGuid = milestone.GetAttributeValue( "Check-inLabel" ).AsGuidOrNull();

                                if ( message.IsNotNullOrWhiteSpace() && labelGuid.HasValue )
                                {
                                    if ( labelGuid != null )
                                    {
                                        var labelCache = KioskLabel.Read( labelGuid.Value );

                                        var mergeObjects = new Dictionary<string, object>();
                                        foreach ( var keyValue in Rock.Lava.LavaHelper.GetCommonMergeFields( null ) )
                                        {
                                            mergeObjects.Add( keyValue.Key, keyValue.Value );
                                        }

                                        mergeObjects.Add( "Person", person.Person );
                                        mergeObjects.Add( "Milestone", milestone );
                                        mergeObjects.Add( "Count", weekendsAttendedCurrentYear );

                                        // Pre merge the message in case it contains lava.
                                        message = message.ResolveMergeFields( mergeObjects );

                                        mergeObjects.Add( "Message", message );

                                        person.SetOptions( labelCache );

                                        var label = new CheckInLabel( labelCache, mergeObjects, person.Person.Id );
                                        label.FileGuid = labelCache.Guid;

                                        //var personGroupTypes = person.GetGroupTypes( true );
                                        //var groupType = personGroupTypes.FirstOrDefault();
                                        if ( groupType != null )
                                        {
                                            groupType.Labels.Add( label );
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return true;

            }

            return false;
        }
    }
}