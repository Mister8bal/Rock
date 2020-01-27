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
                                                        string seasonType = "",
                                                        bool returnRegistrationData = false )
        {
            string scrubbedCampusName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase( campusName.Replace( '-', ' ' ) );

            List<STARSRegistrationModel> response = STARSRegistrationService.GetActiveRegistrations( calendarId, scrubbedCampusName, sport, seasonType );

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
    }
}
