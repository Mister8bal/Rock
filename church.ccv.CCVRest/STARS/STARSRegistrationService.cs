using System;
using System.Collections.Generic;
using System.Linq;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace church.ccv.CCVRest.STARS
{
    class STARSRegistrationService
    {
        public static List<STARSRegistrationModel> GetActiveRegistrations( int calendarId )
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
                foreach ( var occurence in calendarItem.EventItem.EventItemOccurrences )
                {
                    // get linkages that have registration instances and are within start and end date
                    var linkages = occurence.Linkages.Where( a => a.RegistrationInstance.IsNotNull() )
                                                     .Where( a => a.RegistrationInstance.StartDateTime <= DateTime.Now )
                                                     .Where( a => a.RegistrationInstance.EndDateTime >= DateTime.Now );

                    foreach ( var linkage in linkages )
                    {
                        // skip linkage if we already have its occurence in the return object
                        if ( activeRegistrations.Any( a => a.EventOccurrenceId == occurence.Id ) ) {
                            continue;
                        }

                        calendarItem.LoadAttributes();

                        // skip if private
                        string privateLink = calendarItem.AttributeValues["PrivateLink"].Value;

                        if ( privateLink == "True" )
                        {
                            continue;
                        }

                        // skip if any of these attributes are empty
                        string sport = calendarItem.AttributeValues["Sport"].Value;
                        string gender = calendarItem.AttributeValues["Gender"].Value;
                        string division = calendarItem.AttributeValues["Division"].Value;
                        string season = calendarItem.AttributeValues["Season"].Value;

                        if (sport.IsNullOrWhiteSpace() || gender.IsNullOrWhiteSpace() || division.IsNullOrWhiteSpace() || season.IsNullOrWhiteSpace() )
                        {
                            continue;
                        }

                        int slotsAvailable = linkage.RegistrationInstance.MaxAttendees - linkage.RegistrationInstance.Registrations.Count;

                        if ( occurence.Campus == null)
                        {
                            // null campus in occurence means all campuses
                            // create a registration entry for each campus that offers the calendar item sport
                            var campuses = CampusCache.All();

                            foreach ( var campus in campuses )
                            {
                                string campusSports = campus.AttributeValues["Sports"].Value;

                                if ( campusSports.Contains( sport.ToLower() ) )
                                {
                                    // campus found that offers sport of calendar item
                                    STARSRegistrationModel registration = new STARSRegistrationModel()
                                    {
                                        EventOccurrenceId = occurence.Id,
                                        RegistrationInstanceId = linkage.RegistrationInstanceId,
                                        Campus = campus.Name,
                                        Sport = sport,
                                        Gender = gender,
                                        Division = division,
                                        Season = season,
                                        SlotsAvailable = slotsAvailable > 0 ? slotsAvailable : 0,
                                        WaitListEnabled = linkage.RegistrationInstance.RegistrationTemplate.WaitListEnabled
                                    };

                                    activeRegistrations.Add( registration );
                                }
                            }
                        }
                        else
                        {
                            // single campus specified in occurence
                            STARSRegistrationModel registration = new STARSRegistrationModel()
                            {
                                EventOccurrenceId = occurence.Id,
                                RegistrationInstanceId = linkage.RegistrationInstanceId,
                                Campus = occurence.Campus.Name,
                                Sport = sport,
                                Gender = gender,
                                Division = division,
                                Season = season,
                                SlotsAvailable = slotsAvailable > 0 ? slotsAvailable : 0,
                                WaitListEnabled = linkage.RegistrationInstance.RegistrationTemplate.WaitListEnabled
                            };

                            activeRegistrations.Add( registration );
                        }
                    }                          
                }                
            }

            return activeRegistrations;
        }
    }
}
