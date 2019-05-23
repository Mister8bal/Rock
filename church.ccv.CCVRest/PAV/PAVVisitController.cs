using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using church.ccv.CCVCore.PlanAVisit.Model;
using Rock;
using Rock.Data;
using Rock.Rest.Filters;

namespace church.ccv.CCVRest.PAV
{
    public class PAVVisitController : Rock.Rest.ApiControllerBase
    {

        public enum VisitResponse
        {
            NotSet = -1,

            Success,
            AlreadyAttended,
            VisitNotFound
        }


        [System.Web.Http.HttpPost]
        [System.Web.Http.Route( "api/PlanAVisit/Visit" )]
        [Authenticate, Secured]
        public HttpResponseMessage RecordAttendance( int visitId, int attendedCampusId, int attendedScheduleId, DateTime attendedDate )
        {
            Service<PlanAVisit> pavService = new Service<PlanAVisit>( new RockContext() );

            PlanAVisit visit = pavService.Get( visitId );

            if ( visit.IsNotNull() )
            {
                visit.AttendedDate = attendedDate;
                visit.AttendedServiceScheduleId = attendedScheduleId;
            }


            return Common.Util.GenerateResponse( false, VisitResponse.VisitNotFound.ToString(), null );
        }

    }
}
