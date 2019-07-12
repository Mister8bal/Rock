using System.Collections.Generic;
using System.Net.Http;
using Rock.Rest.Filters;

namespace church.ccv.CCVRest.STARS
{
    public partial class STARSController : Rock.Rest.ApiControllerBase
    {
        public enum RegistrationsResponse
        {
            Success,
            NoActiveRegistrations,
            MissingCalendarId
        }

        [Authenticate, Secured]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/STARS/ActiveRegistrations" )]
        public HttpResponseMessage ActiveRegistrations( int calendarId )
        {
            List<STARSRegistrationModel> response = STARSRegistrationService.GetActiveRegistrations( calendarId );

            if ( response.Count > 0 )
            {
                // success
                return Common.Util.GenerateResponse( true, RegistrationsResponse.Success.ToString(), response );
            }

            // default response
            return Common.Util.GenerateResponse( true, RegistrationsResponse.NoActiveRegistrations.ToString(), null );
        }
    }
}
