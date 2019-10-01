using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using church.ccv.CCVCore.PlanAVisit.Model;
using church.ccv.CCVRest.PAV.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace church.ccv.CCVRest.PAV
{
    class PAVScheduledVisitService
    {
        /// <summary>
        /// Returns ScheduledVisits
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public static List<PAVScheduledVisitModel> GetScheduledVisits( DateTime startDate, DateTime endDate )
        {
            List<PAVScheduledVisitModel> scheduledVisits = new List<PAVScheduledVisitModel>();

            RockContext rockContext = new RockContext();

            // services for queries
            var pavTable = new Service<PlanAVisit>( rockContext ).Queryable().AsNoTracking();
            var campusTable = new CampusService( rockContext ).Queryable().AsNoTracking();
            var personAliasTable = new PersonAliasService( rockContext ).Queryable().AsNoTracking();
            var personTable = new PersonService( rockContext ).Queryable().AsNoTracking();
            var scheduleTable = new ScheduleService( rockContext ).Queryable().AsNoTracking();

            // scheduled visits query
            var pavQuery =
                from planAVisit in pavTable
                // adult one
                join adultOnePersonAlias in personAliasTable on planAVisit.AdultOnePersonAliasId equals adultOnePersonAlias.Id into adultOnePersonAliases
                from adultOnePersonAlias in adultOnePersonAliases.DefaultIfEmpty()
                join adultOnePerson in personTable on adultOnePersonAlias.PersonId equals adultOnePerson.Id into adultOnePeople
                from adultOnePerson in adultOnePeople.DefaultIfEmpty()
                // adult two
                join adultTwoPersonAlias in personAliasTable on planAVisit.AdultTwoPersonAliasId equals adultTwoPersonAlias.Id into adultTwoPersonAliases
                from adultTwoPersonAlias in adultTwoPersonAliases.DefaultIfEmpty()
                join adultTwoPerson in personTable on adultTwoPersonAlias.PersonId equals adultTwoPerson.Id into adultTwoPeople
                from adultTwoPerson in adultTwoPeople.DefaultIfEmpty()
                // scheduled visit
                join scheduledCampus in campusTable on planAVisit.ScheduledCampusId equals scheduledCampus.Id into scheduledCampuses
                from scheduledCampus in scheduledCampuses.DefaultIfEmpty()
                join scheduledSchedule in scheduleTable on planAVisit.ScheduledServiceScheduleId equals scheduledSchedule.Id into scheduleSchedules
                from scheduledSchedule in scheduleSchedules.DefaultIfEmpty()
                // attended visit
                join attendedSchedule in scheduleTable on planAVisit.AttendedServiceScheduleId equals attendedSchedule.Id into attendedSchedules
                from attendedSchedule in attendedSchedules.DefaultIfEmpty()
                join attendedCampus in campusTable on planAVisit.AttendedCampusId equals attendedCampus.Id into attendedCampuses
                from attendedCampus in attendedCampuses.DefaultIfEmpty()
                // limit by date
                where planAVisit.ScheduledDate >= startDate && planAVisit.ScheduledDate <= endDate
                select new
                {
                    planAVisit.Id,
                    planAVisit.AdultOnePersonAliasId,
                    AdultOneFirstName = adultOnePerson.FirstName,
                    AdultOneLastName = adultOnePerson.LastName,
                    AdultTwoFirstName = adultTwoPerson.FirstName,
                    AdultTwoLastName = adultTwoPerson.LastName,
                    planAVisit.ScheduledCampusId,
                    ScheduledCampusName = scheduledCampus.Name,
                    planAVisit.ScheduledDate,
                    ScheduledServiceId = planAVisit.ScheduledServiceScheduleId,
                    ScheduledServiceName = scheduledSchedule.Name,
                    planAVisit.BringingChildren,
                    planAVisit.AttendedDate,
                    AttendedServiceId = planAVisit.AttendedServiceScheduleId,
                    AttendedServiceName = attendedSchedule.Name,
                    planAVisit.AttendedCampusId,
                    AttendedCampusName = attendedCampus.Name
                };

            // add visits from query to return object
            foreach ( var item in pavQuery )
            { 
                PAVScheduledVisitModel scheduledVisit = new PAVScheduledVisitModel
                {
                    Id = item.Id,
                    AdultOneFirstName = item.AdultOneFirstName,
                    AdultOneLastName = item.AdultOneLastName,
                    AdultTwoFirstName = item.AdultTwoFirstName.IsNotNull() ? item.AdultTwoFirstName : "",
                    AdultTwoLastName = item.AdultTwoLastName.IsNotNull() ? item.AdultTwoLastName : "",
                    ScheduledCampusId = item.ScheduledCampusId,
                    ScheduledCampusName = item.ScheduledCampusName,
                    ScheduledDate = item.ScheduledDate.HasValue ? item.ScheduledDate.Value.ToString( "MM/dd/yyyy" ) : "",
                    ScheduledServiceId = item.ScheduledServiceId,
                    ScheduledServiceName = item.ScheduledServiceName,
                    AttendedCampusId = item.AttendedCampusId,
                    AttendedCampusName = item.AttendedCampusName,
                    AttendedDate = item.AttendedDate.HasValue ? item.AttendedDate.Value.ToString("MM/dd/yyyy") : "",
                    AttendedServiceId = item.AttendedServiceId,
                    AttendedServiceName = item.AttendedServiceName
                };

                Person person = new PersonAliasService( new RockContext() ).Get( ( int ) item.AdultOnePersonAliasId ).Person;

                PhoneNumber adultOneMobileNumber = person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );

                if ( adultOneMobileNumber.IsNotNull() )
                {
                    // mobile number found add to visit
                    scheduledVisit.AdultOneMobileNumber = adultOneMobileNumber.NumberFormatted;
                }
                else
                {
                    scheduledVisit.AdultOneMobileNumber = "";
                }

                scheduledVisit.Children = new List<PAVChildModel>();

                // if bringing children, add adult one's children
                if ( item.BringingChildren && item.AdultOnePersonAliasId > 0 )
                {
                    var familyMembers = person.GetFamilyMembers( false, null );

                    if ( familyMembers.Count() > 0 )
                    {
                        foreach ( var familyMember in familyMembers )
                        {
                            var groupTypeRole = familyMember.Person.GetFamilyRole();

                            if ( groupTypeRole != null && groupTypeRole.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid() )
                            {
                                PAVChildModel child = new PAVChildModel
                                {
                                    FirstName = familyMember.Person.FirstName,
                                    Age = familyMember.Person.Age,
                                    BirthDate = familyMember.Person.BirthDate.HasValue ? familyMember.Person.BirthDate.Value.ToString( "MM/dd/yyyy" ) : "",
                                    Grade = familyMember.Person.GradeFormatted
                                };

                                scheduledVisit.Children.Add( child );
                            }
                        }
                    }
                }

                scheduledVisits.Add( scheduledVisit );
            }

            return scheduledVisits;
        }

        /// <summary>
        /// Returns campuses that have valid service times
        /// </summary>
        /// <returns></returns>
        public static List<PAVCampusModel> GetCampuses()
        {
            List<PAVCampusModel> pavCampuses = new List<PAVCampusModel>();
           
            // build schedule lookup list to get schedule Id
            ScheduleService scheduleService = new ScheduleService( new RockContext() );

            var scheduleLookupList = scheduleService.Queryable().Where( a => a.Name != null && a.Name != "" ).ToList().Select( a => new
            {
                a.Id,
                a.Name,
                a.WeeklyDayOfWeek
            } );

            // loop through active campuses and add campuses that have valid service time schedules
            List<CampusCache> campuses = CampusCache.All( false );
            
            foreach ( var campus in campuses )
            {
                if ( campus.ServiceTimes.Count > 0 )
                {
                    PAVCampusModel pavCampus = new PAVCampusModel
                    {
                        Id = campus.Id,
                        Name = campus.Name,
                        ServiceTimes = new List<PAVServiceTimeModel>()
                    };

                    foreach ( var serviceTime in campus.ServiceTimes )
                    {
                        // look for a schedule by schedule name
                        string serviceTimeName = string.Format( "{0} {1}", serviceTime.Day, serviceTime.Time).Trim();

                        var scheduleLookup = scheduleLookupList.FirstOrDefault( a => a.Name.RemoveSpecialCharacters() == serviceTimeName.RemoveSpecialCharacters() );

                        if ( scheduleLookup.IsNotNull() )
                        {
                            // schedule found
                            
                            // build service time string
                            string[] scheduleNameArray = scheduleLookup.Name.Split( ' ' );

                            string scheduleTime = scheduleNameArray[1];

                            PAVServiceTimeModel pavServiceTime = new PAVServiceTimeModel
                            {
                                ScheduleId = scheduleLookup.Id,
                                Day = serviceTime.Day,
                                Name = scheduleLookup.Name,
                                Time = scheduleTime
                            };

                            pavCampus.ServiceTimes.Add( pavServiceTime );
                        }
                    }

                    if ( pavCampus.ServiceTimes.Count > 0 )
                    {
                        // campus has schedules, include
                        pavCampuses.Add( pavCampus );
                    }
                }
            }

            return pavCampuses;
        }

        public enum RecordAttendedResponse
        {
            Success,
            AlreadyAttended,
            VisitNotFound,
            Failed
        }

        /// <summary>
        /// Update a family members First Visit Date attribute if not already set. 
        /// </summary>
        /// <param name="visit"></param>
        /// <param name="familyMember"></param>
        /// <returns type="bool">Whether or not the update succeeded.  If any family member update fails,
        /// will return false, however will not halt the process.  All other family member updates may still
        /// succeed
        /// </returns>
        public static bool UpdateFamilyMemberVisitDate(PlanAVisit visit, GroupMember familyMember)
        {
            RockContext rockContext = new RockContext();
            AttributeValueService avService = new AttributeValueService( rockContext );

            try
            {
                
                // get the first campus visit person attribute for adult one
                int firstCampusVisit_AttributeId = 717;

                AttributeValue avFirstCampusVisit = avService.Queryable().Where( av => av.EntityId == familyMember.PersonId && av.AttributeId == firstCampusVisit_AttributeId ).SingleOrDefault();

                if ( avFirstCampusVisit == null )
                {
                    // attribute does not yet exist, create before proceeding
                    avFirstCampusVisit = new AttributeValue
                    {
                        EntityId = familyMember.PersonId,
                        AttributeId = firstCampusVisit_AttributeId
                    };
                    avService.Add( avFirstCampusVisit );
                }

                // only update value if current value does not exist so we dont lose previous first visit
                if ( avFirstCampusVisit.Value.IsNullOrWhiteSpace() )
                {
                    avFirstCampusVisit.Value = visit.AttendedDate.ToString();
                }

                rockContext.SaveChanges();

            }
            catch ( Exception )
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Update a family members First Visit Date attribute if not already set. 
        /// </summary>
        /// <param name="personId">The person id to add the note for</param>
        /// <param name="visit">The visit object related to the note.</param>
        /// <param name="visit">The person alias id of the logged in user who marked the visit as attened</param>
        /// <returns></returns>
        public static void AddAttendedNote( int personId, PlanAVisit visit, int? personAliasId = null )
        {

            RockContext rockContext = new RockContext();

            var noteType = NoteTypeCache.Read( Rock.SystemGuid.NoteType.PERSON_TIMELINE_NOTE.AsGuid() );
            var noteService = new NoteService( rockContext );
            var campusService = new CampusService( rockContext );
            PersonService personService = new PersonService(rockContext);
            ScheduleService scheduleService = new ScheduleService( rockContext );

            Person notePerson = personService.Get( personId );
            Campus visitCampus = campusService.Get( visit.AttendedCampusId.Value );
            Schedule visitSchedule = scheduleService.Get( visit.ScheduledServiceScheduleId );
            // Ensure that person id provided is a valid person record.
            // This check is mainly to ensure we don't end up
            // with orphaned db records in scenarios where the person id
            // is invalid, or no longer exists.
            if ( notePerson.IsNotNull() )
            {
                var note = new Note();
                note.IsSystem = false;
                note.IsAlert = false;
                note.IsPrivateNote = false;
                note.NoteTypeId = noteType.Id;
                note.EntityId = notePerson.Id;
                note.Text = string.Format("Plan A Visit Attended [{0}] [{1}] [{2}]", visit.AttendedDate.Value.ToString( "MM/dd/yyyy" ), visitSchedule.Name, visitCampus.Name ); 
                if ( personAliasId.HasValue )
                {
                    note.CreatedByPersonAliasId = personAliasId;
                }
                noteService.Add( note );

                rockContext.SaveChanges();
            }

        }

        /// <summary>
        /// Record attended information to a visit
        /// </summary>
        /// <param name="visitId"></param>
        /// <param name="attendedScheduleId"></param>
        /// <param name="attendedCampusId"></param>
        /// <param name="attendedDate"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static RecordAttendedResponse RecordAttended( int visitId, int attendedCampusId, int attendedScheduleId, DateTime attendedDate, out string message, int? submitterAliasId = null )
        {
            RockContext rockContext = new RockContext();

            Service<PlanAVisit> pavService = new Service<PlanAVisit>( rockContext );
            PersonAliasService personAliasService = new PersonAliasService( rockContext );
            
            PersonService pService = new PersonService( rockContext );
            IEnumerable<GroupMember> familyMembers;
            bool success = true;

            PlanAVisit visit = pavService.Get( visitId );

            if ( visit.IsNotNull() )
            {
                if ( !visit.AttendedDate.HasValue )
                {
                    // we have a valid visit with no attended date
                    // get adult one as a person
                    PersonAlias adultOne = personAliasService.Get( visit.AdultOnePersonAliasId );
                    Group family = pService.GetFamilies( adultOne.PersonId ).FirstOrDefault();

                    familyMembers = family.ActiveMembers();

                    // update attended info for visit
                    visit.AttendedDate = attendedDate;
                    visit.AttendedServiceScheduleId = attendedScheduleId;
                    visit.AttendedCampusId = attendedCampusId;

                    foreach (GroupMember familyMember in familyMembers)
                    {

                        if(!UpdateFamilyMemberVisitDate( visit, familyMember ) )
                        {
                            success = false;
                        }
                        else
                        {
                            AddAttendedNote( familyMember.PersonId, visit, submitterAliasId );
                        }

                    }

                    rockContext.SaveChanges();
                    
                    if ( success )
                    {
                        
                        message = "Visit updated successfully";
                        return RecordAttendedResponse.Success;
                    }
                    else
                    {
                        message = "Failed to update visit. One or more family members failed to update successfully.";
                        return RecordAttendedResponse.Failed;
                    }

                }
                else
                {
                    message = "Visit has already been marked attended.";

                    return RecordAttendedResponse.AlreadyAttended;
                }
            }

            // default response
            message = "Failed to load visit";

            return RecordAttendedResponse.VisitNotFound;
        }
    }
}
