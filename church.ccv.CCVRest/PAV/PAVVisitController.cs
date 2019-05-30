using System;
using System.Net.Http;
using church.ccv.CCVCore.PlanAVisit.Model;
using Rock;
using Rock.Data;
using Rock.Rest.Filters;

namespace church.ccv.CCVRest.PAV
{
    public partial class PlanAVisitController : Rock.Rest.ApiControllerBase
    {
        public enum VisitResponse
        {
            Success,
            AlreadyAttended,
            VisitNotFound,
            Failed
        }

        [System.Web.Http.HttpPost]
        [System.Web.Http.Route( "api/PlanAVisit/Attended" )]
        [Authenticate, Secured]
        public HttpResponseMessage RecordAttended( int visitId, int attendedCampusId, int attendedScheduleId, DateTime attendedDate )
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

                        return Common.Util.GenerateResponse( true, VisitResponse.Success.ToString(), null );
                    }
                    catch ( Exception )
                    {
                        return Common.Util.GenerateResponse( false, VisitResponse.Failed.ToString(), null );
                    }
                }
                else
                {
                    return Common.Util.GenerateResponse( false, VisitResponse.AlreadyAttended.ToString(), null );
                }
            }

            // default response - visit not found
            return Common.Util.GenerateResponse( false, VisitResponse.VisitNotFound.ToString(), null );
        }
    }
}
