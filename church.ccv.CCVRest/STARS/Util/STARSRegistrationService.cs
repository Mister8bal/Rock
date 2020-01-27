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
        /// <summary>
        /// Return a list of regular active registrations
        /// </summary>
        /// <param name="calendarId"></param>
        /// <param name="requestedCampus"></param>
        /// <param name="requestedSport"></param>
        /// <returns></returns>
        public static List<STARSRegistrationModel> GetActiveRegistrations( int calendarId, string requestedCampus = "", string requestedSport = "", string requestedSeasonType = "" )
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

                        // skip if item is hidden from the grid
                        // default to true if no value is passed
                        string avShowInRegisterGrid = calendarItem.GetAttributeValue( "ShowInRegisterGrid" );
                        bool showInRegisterGrid = ( avShowInRegisterGrid.IsNullOrWhiteSpace() || avShowInRegisterGrid.AsBoolean() ) == true ? true : false;

                        if ( showInRegisterGrid == false )
                        {
                            continue;
                        }

                        // get the values of attributes
                        string sport = calendarItem.GetAttributeValue( "Sport" );
                        string gender = calendarItem.GetAttributeValue( "Gender" );
                        string division = calendarItem.GetAttributeValue( "Division" );
                        string grades = calendarItem.GetAttributeValue( "Grades" );
                        string season = calendarItem.GetAttributeValue( "Season" );
                        string seasonType = calendarItem.GetAttributeValue( "SeasonType" );

                        // skip if any of these values are missing
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

                        // default season type to League if a type wasnt passed
                        if ( seasonType.IsNullOrWhiteSpace() )
                        {
                            seasonType = "League";
                        }

                        if ( occurrence.Campus == null )
                        {
                            // null campus in occurence means all campuses
                            var campuses = CampusCache.All();

                            foreach ( var campus in campuses )
                            {
                                string campusSports = campus.GetAttributeValue( "Sports" );

                                // we only want the campus if it offers the sport
                                // and if sport matches requestedSport or all sports requested
                                // and if campus name matches the requestedCampus or all campuses requested
                                if ( campusSports.Contains( sport.ToLower() ) &&
                                     ( sport.Contains( requestedSport.ToLower() ) || requestedSport == "" ) &&
                                     ( requestedCampus.ToLower() == campus.Name.ToLower() || requestedCampus == "" ) &&
                                     ( requestedSeasonType.ToLower() == seasonType.ToLower() || requestedSeasonType == "" ) )
                                {
                                    // add registration to activeRegistrations
                                    var registration = CreateSTARSRegistration( occurrence.Id,
                                                                                occurrence.EventItem.Summary,
                                                                                occurrence.NextStartDateTime,
                                                                                linkage.RegistrationInstanceId,
                                                                                campus.Name,
                                                                                sport,
                                                                                // if splitGender is true, use "Boys" girls will be added below otherwise use gender passed
                                                                                splitGender ? "Boys" : gender,
                                                                                division,
                                                                                grades,
                                                                                season,
                                                                                seasonType,
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
                                                                                         seasonType,
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
                                  ( requestedCampus.ToLower() == occurrence.Campus.Name.ToLower() || requestedCampus == "" ) &&
                                  ( requestedSeasonType.ToLower() == seasonType.ToLower() || requestedSeasonType == "" ) )
                        {
                            // add registration to active registrations
                            var registration = CreateSTARSRegistration( occurrence.Id,
                                                                        occurrence.EventItem.Summary,
                                                                        occurrence.NextStartDateTime,
                                                                        linkage.RegistrationInstanceId,
                                                                        occurrence.Campus.Name,
                                                                        sport,
                                                                        // if splitGender is true, use "Boys" girls will be added below otherwise use gender passed
                                                                        splitGender ? "Boys" : gender,
                                                                        division,
                                                                        grades,
                                                                        season,
                                                                        seasonType,
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
                                                                                 seasonType,
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
                                                                       string seasonType,
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
                SeasonType = seasonType,
                SlotsAvailable = slotsAvailable,
                WaitListEnabled = waitListEnabled,
                Cost = cost
            };
        }
    }
}
