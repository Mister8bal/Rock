using System;
using System.Collections.Generic;
using System.Net.Http;
using Rock.Rest.Filters;
using church.ccv.CCVRest.STARS.Model;
using church.ccv.CCVRest.STARS.Util;

namespace church.ccv.CCVRest.STARS.Controllers
{
    public partial class STARSController : Rock.Rest.ApiControllerBase
    {
        public enum FieldStatusResponse
        {
            Success,
            Failed
        }

        [Authenticate, Secured]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/STARS/FieldStatus" )]
        public HttpResponseMessage FieldStatus()
        {
            List<STARSFieldStatusModel> response = STARSFieldStatusService.GetFieldStatus();

            if ( response.Count > 0 )
            {
                return Common.Util.GenerateResponse( true, FieldStatusResponse.Success.ToString(), response );
            }

            return Common.Util.GenerateResponse( true, FieldStatusResponse.Failed.ToString(), null );
        }
    }
}
