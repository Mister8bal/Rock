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
                join adultTwoPersonAlias in personAliasTable on planAVisit.AdultOnePersonAliasId equals adultTwoPersonAlias.Id into adultTwoPersonAliases
                from adultTwoPersonAlias in adultOnePersonAliases.DefaultIfEmpty()
                join adultTwoPerson in personTable on adultOnePersonAlias.PersonId equals adultTwoPerson.Id into adultTwoPeople
                from adultTwoPerson in adultOnePeople.DefaultIfEmpty()
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
                    AdultTwoFirstName = item.AdultTwoFirstName,
                    AdultTwoLastName = item.AdultTwoLastName,
                    ScheduledCampusId = item.ScheduledCampusId,
                    ScheduledCampusName = item.ScheduledCampusName,
                    ScheduledDate = item.ScheduledDate,
                    ScheduledServiceId = item.ScheduledServiceId,
                    ScheduledServiceName = item.ScheduledServiceName,
                    AttendedCampusId = item.AttendedCampusId,
                    AttendedCampusName = item.AttendedCampusName,
                    AttendedDate = item.AttendedDate,
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
                
                // if bringing children, add adult ones children
                if ( item.BringingChildren && item.AdultOnePersonAliasId > 0 )
                {
                    scheduledVisit.Children = new List<PAVChildModel>();

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
                                    BirthDate = familyMember.Person.BirthDate,
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
                            // schedule found, add service time to campus
                            PAVServiceTimeModel pavServiceTime = new PAVServiceTimeModel
                            {
                                ScheduleId = scheduleLookup.Id,
                                Day = serviceTime.Day,
                                Name = scheduleLookup.Name
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
        /// Record attended information to a visit
        /// </summary>
        /// <param name="visitId"></param>
        /// <param name="attendedScheduleId"></param>
        /// <param name="attendedCampusId"></param>
        /// <param name="attendedDate"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static RecordAttendedResponse RecordAttended( int visitId, int attendedCampusId, int attendedScheduleId, DateTime attendedDate, out string message )
        {
            RockContext rockContext = new RockContext();

            Service<PlanAVisit> pavService = new Service<PlanAVisit>( rockContext );

            PlanAVisit visit = pavService.Get( visitId );

            if ( visit.IsNotNull() )
            {
                if ( !visit.AttendedDate.HasValue )
                {
                    // we have a valid visit with no attended date
                    // try to update attended info for visit
                    try
                    {
                        visit.AttendedDate = attendedDate;
                        visit.AttendedServiceScheduleId = attendedScheduleId;
                        visit.AttendedCampusId = attendedCampusId;

                        rockContext.SaveChanges();

                        message = "Visit updated successfully";

                        return RecordAttendedResponse.Success;
                    }
                    catch ( Exception )
                    {
                        message = "Failed to update visit.";

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
