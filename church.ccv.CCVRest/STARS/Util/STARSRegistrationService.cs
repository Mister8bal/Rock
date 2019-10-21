using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using church.ccv.CCVRest.STARS.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace church.ccv.CCVRest.STARS.Util
{
    class STARSRegistrationService
    {
        // attribute ids for stars calendar attributes
        private static int _attributeIdLinkType = 95183;

        /// <summary>
        /// Return a list of regular active registrations
        /// </summary>
        /// <param name="calendarId"></param>
        /// <param name="requestedCampus"></param>
        /// <param name="requestedSport"></param>
        /// <returns></returns>
        public static List<STARSRegistrationModel> GetActiveRegistrations( int calendarId, string requestedCampus = "", string requestedSport = "" )
        {
            // return object
            List<STARSRegistrationModel> activeRegistrations = new List<STARSRegistrationModel>();

            // get the stars calendar and active calendar items that have occurrences
            EventCalendarService calendarService = new EventCalendarService( new RockContext() );
            EventCalendar starsCalendar = calendarService.Get( calendarId );

            var activeCalendarItems = starsCalendar.EventCalendarItems.Where( a => a.EventItem.IsActive == true )
                                                                      .Where( a => a.EventItem.EventItemOccurrences.Count > 0 );

            // look for calendar items with registrations
            foreach ( var calendarItem in activeCalendarItems )
            {
                foreach ( var occurrence in calendarItem.EventItem.EventItemOccurrences )
                {
                    // get linkages that have registration instances and are within start and end date
                    var linkages = occurrence.Linkages.Where( a => a.RegistrationInstance.IsNotNull() )
                                                     .Where( a => a.RegistrationInstance.StartDateTime <= DateTime.Now )
                                                     .Where( a => a.RegistrationInstance.EndDateTime >= DateTime.Now );

                    foreach ( var linkage in linkages )
                    {
                        // skip linkage if we already have its occurence in the return object
                        if ( activeRegistrations.Any( a => a.EventOccurrenceId == occurrence.Id ) ) {
                            continue;
                        }

                        // To future self, if people complain about performance, probably do away with LoadAttributes
                        // and do SQL joins to get the info needed
                        calendarItem.LoadAttributes();

                        // skip if not regular link type
                        string linkType = calendarItem.AttributeValues["LinkType"].Value;

                        if ( linkType != "Regular" )
                        {
                            continue;
                        }

                        // skip if any of these attributes are empty
                        string sport = calendarItem.AttributeValues["Sport"].Value;
                        string gender = calendarItem.AttributeValues["Gender"].Value;
                        string division = calendarItem.AttributeValues["Division"].Value;
                        string grades = calendarItem.AttributeValues["Grades"].Value;
                        string season = calendarItem.AttributeValues["Season"].Value;

                        if ( sport.IsNullOrWhiteSpace() || 
                             gender.IsNullOrWhiteSpace() || 
                             division.IsNullOrWhiteSpace() || 
                             season.IsNullOrWhiteSpace() ||
                             grades.IsNullOrWhiteSpace() )
                        {
                            continue;
                        }

                        // get cost
                        decimal? cost = linkage.RegistrationInstance.Cost > 0 ? linkage.RegistrationInstance.Cost : 0;

                        // -1 indicates unlimited slots available, MaxAttendees 0 = unlimited slots
                        int slotsAvailable = -1;

                        if ( linkage.RegistrationInstance.MaxAttendees > 0 )
                        {
                            // There is a MaxAttnedees number set, calculate slots available, ensure no negative number
                            slotsAvailable = Math.Max( 0, ( linkage.RegistrationInstance.MaxAttendees - linkage.RegistrationInstance.Registrations.Count ) );
                        }

                        // "Boys & Girls" gender needs to be returned as 2 event items to display and filter properly
                        // I know, not best idea having to depend on a string
                        bool splitGender = gender == "Boys & Girls";

                        if ( occurrence.Campus == null )
                        {
                            // null campus in occurence means all campuses
                            var campuses = CampusCache.All();

                            foreach ( var campus in campuses )
                            {
                                string campusSports = campus.AttributeValues["Sports"].Value;

                                // we only want the campus if it offers the sport
                                // and if sport matches requestedSport or all sports requested
                                // and if campus name matches the requestedCampus or all campuses requested
                                if ( campusSports.Contains( sport.ToLower() ) &&
                                     ( sport.Contains( requestedSport.ToLower() ) || requestedSport == "" ) &&
                                     ( requestedCampus == campus.Name || requestedCampus == "" ) )
                                {
                                    // add registration to activeRegistrations
                                    var registration = CreateSTARSRegistration( occurrence.Id,
                                                                                occurrence.EventItem.Summary,
                                                                                occurrence.NextStartDateTime,
                                                                                linkage.RegistrationInstanceId,
                                                                                campus.Name,
                                                                                sport,
                                                                                splitGender ? "Boys" : gender,
                                                                                division,
                                                                                grades,
                                                                                season,
                                                                                slotsAvailable,
                                                                                linkage.RegistrationInstance.RegistrationTemplate.WaitListEnabled,
                                                                                cost );

                                    activeRegistrations.Add( registration );

                                    // if splitGender, add same registration, but a 2nd time for girls
                                    if ( splitGender )
                                    {
                                        var girlsRegistration = CreateSTARSRegistration( occurrence.Id,
                                                                                         occurrence.EventItem.Summary,
                                                                                         occurrence.NextStartDateTime,
                                                                                         linkage.RegistrationInstanceId,
                                                                                         campus.Name,
                                                                                         sport,
                                                                                         "Girls",
                                                                                         division,
                                                                                         grades,
                                                                                         season,
                                                                                         slotsAvailable,
                                                                                         linkage.RegistrationInstance.RegistrationTemplate.WaitListEnabled,
                                                                                         cost );
                                           
                                        activeRegistrations.Add( girlsRegistration );
                                    }
                                }
                            }
                        }
                        // we only want the occurence if
                        // sport matches requestedSport or all sports requested
                        // and occurrence campus matches requestedCampus or all campuses requested
                        else if ( ( sport.Contains( requestedSport.ToLower() ) || requestedSport == "" ) &&
                                  ( requestedCampus == occurrence.Campus.Name || requestedCampus == "" ) )
                        {
                            // add registration to active registrations
                            var registration = CreateSTARSRegistration( occurrence.Id,
                                                                        occurrence.EventItem.Summary,
                                                                        occurrence.NextStartDateTime,
                                                                        linkage.RegistrationInstanceId,
                                                                        occurrence.Campus.Name,
                                                                        sport,
                                                                        splitGender ? "Boys" : gender,
                                                                        division,
                                                                        grades,
                                                                        season,
                                                                        slotsAvailable,
                                                                        linkage.RegistrationInstance.RegistrationTemplate.WaitListEnabled,
                                                                        cost);
                           
                            activeRegistrations.Add( registration );

                            // if splitGender, add same registration, but a 2nd time for girls
                            if ( splitGender )
                            {
                                var girlsRegistration = CreateSTARSRegistration( occurrence.Id,
                                                                                 occurrence.EventItem.Summary,
                                                                                 occurrence.NextStartDateTime,
                                                                                 linkage.RegistrationInstanceId,
                                                                                 occurrence.Campus.Name,
                                                                                 sport,
                                                                                 "Girls",
                                                                                 division,
                                                                                 grades,
                                                                                 season,
                                                                                 slotsAvailable,
                                                                                 linkage.RegistrationInstance.RegistrationTemplate.WaitListEnabled,
                                                                                 cost );

                                activeRegistrations.Add( girlsRegistration );
                            }
                        }
                    }                          
                }                
            }

            return activeRegistrations;
        }        

        /// <summary>
        /// Return a list of camps active registrations
        /// </summary>
        /// <param name="calendarId"></param>
        /// <param name="requestedCampus"></param>
        /// <param name="requestedSport"></param>
        /// <returns></returns>
        public static List<STARSRegistrationModel> GetActiveCamps( int calendarId, string requestedCampus = "", string requestedSport = "" )
        {
            // return object
            List<STARSRegistrationModel> activeCamps = new List<STARSRegistrationModel>();

            RockContext rockContext = new RockContext();

            var calendarItemTable = new EventCalendarItemService( rockContext ).Queryable().AsNoTracking();
            var avTable = new AttributeValueService( rockContext ).Queryable().AsNoTracking();

            var campsQuery =
                from calendarItem in calendarItemTable
                join avLinkType in avTable on calendarItem.Id equals avLinkType.EntityId
                where 
                    calendarItem.EventCalendarId == calendarId &&
                    avLinkType.AttributeId == _attributeIdLinkType &&
                    avLinkType.Value == "Camp" 
                select new
                {
                    calendarItem.EventItem
                };

            foreach ( var item in campsQuery )
            {
                foreach ( var occurrence in item.EventItem.EventItemOccurrences )
                {
                    // get linkages that have registration instances and are within start and end date
                    var linkages = occurrence.Linkages.Where( a => a.RegistrationInstance.IsNotNull() )
                                                     .Where( a => a.RegistrationInstance.StartDateTime <= DateTime.Now )
                                                     .Where( a => a.RegistrationInstance.EndDateTime >= DateTime.Now );

                    foreach ( var linkage in linkages )
                    {
                        // skip linkage if we already have its occurence in the return object
                        if ( activeCamps.Any( a => a.EventOccurrenceId == occurrence.Id ) )
                        {
                            continue;
                        }

                        // To future self, if people complain about performance, probably do away with LoadAttributes
                        // and do SQL joins to get the info needed
                        occurrence.EventItem.LoadAttributes();

                        // skip if any of these attributes are empty
                        string sport = occurrence.EventItem.AttributeValues["Sport"].Value;
                        string season = occurrence.EventItem.AttributeValues["Season"].Value;

                        if ( sport.IsNullOrWhiteSpace() ||
                             season.IsNullOrWhiteSpace() )
                        {
                            continue;
                        }

                        // get cost
                        decimal? cost = linkage.RegistrationInstance.Cost > 0 ? linkage.RegistrationInstance.Cost : 0;

                        // -1 indicates unlimited slots available, MaxAttendees 0 = unlimited slots
                        int slotsAvailable = -1;

                        if ( linkage.RegistrationInstance.MaxAttendees > 0 )
                        {
                            // There is a MaxAttnedees number set, calculate slots available, ensure no negative number
                            slotsAvailable = Math.Max( 0, ( linkage.RegistrationInstance.MaxAttendees - linkage.RegistrationInstance.Registrations.Count ) );
                        }

                        if ( occurrence.Campus == null )
                        {
                            // null campus in occurence means all campuses
                            var campuses = CampusCache.All();

                            foreach ( var campus in campuses )
                            {
                                string campusSports = campus.AttributeValues["Sports"].Value;

                                // we only want the campus if it offers the sport
                                // and if requested sport matches sport or requestSport = ""
                                // and if requested campus matches campus or requestedCampus = ""
                                if ( campusSports.Contains( sport.ToLower() ) &&
                                     ( sport.Contains( requestedSport.ToLower() ) || requestedSport == "" ) &&
                                     ( requestedCampus == campus.Name || requestedCampus == "" ) )

                                {
                                    // create a camp registration
                                    var campRegistration = CreateSTARSRegistration( occurrence.Id,
                                                                                    occurrence.EventItem.Summary,
                                                                                    occurrence.NextStartDateTime,
                                                                                    linkage.RegistrationInstanceId,
                                                                                    campus.Name,
                                                                                    sport,
                                                                                    "",
                                                                                    "",
                                                                                    "",
                                                                                    season,
                                                                                    slotsAvailable,
                                                                                    linkage.RegistrationInstance.RegistrationTemplate.WaitListEnabled,
                                                                                    cost );

                                    activeCamps.Add( campRegistration );
                                    
                                }
                            }                            
                        }
                        // we only want the sport requested unless requestesSport = ""
                        // and we only want the requested campus unless requestCampus = ""
                        else if ( ( sport.Contains( requestedSport.ToLower() ) || requestedSport == "" ) &&
                                  ( requestedCampus == occurrence.Campus.Name || requestedCampus == "" ) )
                        {
                            // create camp registration
                            var campRegistration = CreateSTARSRegistration( occurrence.Id,
                                                                            occurrence.EventItem.Summary,
                                                                            occurrence.NextStartDateTime,
                                                                            linkage.RegistrationInstanceId,
                                                                            occurrence.Campus.Name,
                                                                            sport,
                                                                            "",
                                                                            "",
                                                                            "",
                                                                            season,
                                                                            slotsAvailable,
                                                                            linkage.RegistrationInstance.RegistrationTemplate.WaitListEnabled,
                                                                            cost );

                            activeCamps.Add( campRegistration );                            
                        }
                    }
                }
            }

            return activeCamps;
        }

        /// <summary>
        /// Return a StarsRegistration Model object
        /// </summary>
        /// <param name="eventOccurrenceId"></param>
        /// <param name="registrationInstanceId"></param>
        /// <param name="campusName"></param>
        /// <param name="sport"></param>
        /// <param name="gender"></param>
        /// <param name="division"></param>
        /// <param name="grades"></param>
        /// <param name="season"></param>
        /// <param name="slotsAvailable"></param>
        /// <param name="waitListEnabled"></param>
        /// <param name="cost"></param>
        /// <returns></returns>
        private static STARSRegistrationModel CreateSTARSRegistration( int eventOccurrenceId,
                                                                       string eventSummary,
                                                                       DateTime? eventOccurrenceDate,
                                                                       int? registrationInstanceId,
                                                                       string campusName,
                                                                       string sport,
                                                                       string gender,
                                                                       string division,
                                                                       string grades,
                                                                       string season,
                                                                       int slotsAvailable,
                                                                       bool waitListEnabled,
                                                                       decimal? cost )
        {
            return new STARSRegistrationModel()
            {
                EventOccurrenceId = eventOccurrenceId,
                EventSummary = eventSummary,
                EventOccurrenceDate = eventOccurrenceDate,
                RegistrationInstanceId = registrationInstanceId,
                Campus = campusName,
                Sport = sport,
                Gender = gender,
                Division = division,
                Grades = grades,
                Season = season,
                SlotsAvailable = slotsAvailable,
                WaitListEnabled = waitListEnabled,
                Cost = cost
            };
        }
    }
}
