using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using church.ccv.CCVRest.STARS.Model;
using church.ccv.CCVRest.STARS.Util;
using Rock.Rest.Filters;

namespace church.ccv.CCVRest.STARS.Controllers
{
    public partial class STARSController : Rock.Rest.ApiControllerBase
    {
        public enum RegistrationsResponse
        {
            Success,
            NoActiveRegistrations
        }

        [Authenticate, Secured]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/STARS/ActiveRegistrations" )]
        public HttpResponseMessage ActiveRegistrations( int calendarId, 
                                                        string campusName = "", 
                                                        string sport = "", 
                                                        bool returnRegistrationData = false )
        {
            string scrubbedCampusName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase( campusName.Replace( '-', ' ' ) );

            List<STARSRegistrationModel> response = STARSRegistrationService.GetActiveRegistrations( calendarId, scrubbedCampusName, sport );

            if ( response.Count > 0 )
            {
                // success
                if ( returnRegistrationData )
                {
                    // include registration data in response
                    return Common.Util.GenerateResponse( true, RegistrationsResponse.Success.ToString(), response );
                }
                else
                {
                    // dont include registration data in response
                    return Common.Util.GenerateResponse( true, RegistrationsResponse.Success.ToString(), null );
                }
            }

            // default response
            return Common.Util.GenerateResponse( true, RegistrationsResponse.NoActiveRegistrations.ToString(), null );
        }

        public enum CampsResponse
        {
            Success,
            NoActiveCamps
        }

        [Authenticate, Secured]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/STARS/ActiveCamps" )]
        public HttpResponseMessage ActiveCamps( int calendarId, 
                                                string campusName = "", 
                                                string sport = "", 
                                                bool returnCampsData = false )
        {
            string scrubbedCampusName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase( campusName.Replace( '-', ' ' ) );

            List<STARSRegistrationModel> response = STARSRegistrationService.GetActiveCamps( calendarId, scrubbedCampusName, sport );

            if ( response.Count > 0 )
            {
                // success
                if ( returnCampsData )
                {
                    // include camps data in response
                    return Common.Util.GenerateResponse( true, CampsResponse.Success.ToString(), response );
                }
                else
                {
                    // dont include camps data in response
                    return Common.Util.GenerateResponse( true, CampsResponse.Success.ToString(), null );
                }
            }

            // default response
            return Common.Util.GenerateResponse( true, CampsResponse.NoActiveCamps.ToString(), null );
        }
    }
}
