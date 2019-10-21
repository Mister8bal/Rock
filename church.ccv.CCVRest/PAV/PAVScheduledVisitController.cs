using System;
using System.Net.Http;
using church.ccv.CCVRest.PAV.Model;
using Rock.Rest.Filters;

namespace church.ccv.CCVRest.PAV
{
    public partial class PlanAVisitController : Rock.Rest.ApiControllerBase
    {
        public enum ScheduledVisitsResponse
        {
            Success,
            NoScheduledVisits,
            Failed
        }

        [Authenticate, Secured]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/PlanAVisit/ScheduledVisits" )]
        public HttpResponseMessage ScheduledVisits( DateTime startDate, DateTime endDate )
        {
            PAVScheduledVisitsModel response = new PAVScheduledVisitsModel
            {
                ScheduledVisits = PAVScheduledVisitService.GetScheduledVisits( startDate, endDate ),
                Campuses = PAVScheduledVisitService.GetCampuses()
            };

            if ( response.Campuses.Count == 0 )
            {
                // failed
                return Common.Util.GenerateResponse( false, ScheduledVisitsResponse.Failed.ToString(), "Failed to get campuses" );
            }

            if ( response.ScheduledVisits.Count > 0 )
            {
                // success
                return Common.Util.GenerateResponse( true, ScheduledVisitsResponse.Success.ToString(), response );
            }

            // default response
            return Common.Util.GenerateResponse( true, ScheduledVisitsResponse.NoScheduledVisits.ToString(), null );
        }


        [System.Web.Http.HttpPost]
        [System.Web.Http.Route( "api/PlanAVisit/Attended" )]
        [Authenticate, Secured]
        public HttpResponseMessage RecordAttended( int visitId, int attendedCampusId, int attendedScheduleId, DateTime attendedDate, int? personAliasId = null )
        {
            string message = String.Empty;

            PAVScheduledVisitService.RecordAttendedResponse updateResponse = PAVScheduledVisitService.RecordAttended( visitId, attendedCampusId, attendedScheduleId, attendedDate, out message, personAliasId );
                       
            return Common.Util.GenerateResponse( updateResponse == PAVScheduledVisitService.RecordAttendedResponse.Success, updateResponse.ToString(), message );
        }
    }
}
