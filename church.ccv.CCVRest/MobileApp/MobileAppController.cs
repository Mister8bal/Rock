using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using church.ccv.Actions;
using church.ccv.CCVRest.Common.Model;
using church.ccv.CCVRest.MobileApp.Model;
using Newtonsoft.Json;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest.Filters;
using Rock.Web.Cache;

namespace church.ccv.CCVRest.MobileApp
{
    public class NewMobileAppController : Rock.Rest.ApiControllerBase
    {
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Person" )]
        [Authenticate, Secured]
        public HttpResponseMessage Person( string userID )
        {
            RockContext rockContext = new RockContext();
            PersonService personService = new PersonService( rockContext );
            UserLoginService userLoginService = new UserLoginService( rockContext );

            int? personId = userLoginService.Queryable()
                .Where( u => u.UserName.Equals( userID ) )
                .Select( a => a.PersonId )
                .FirstOrDefault();

            ResponseModel response = new ResponseModel();
            if ( personId.HasValue )
            {
                PersonModel personModel = Util.Util.GetPersonModel( personId.Value );

                response.Data = personModel;
                response.Success = true;
            }
            else
            {
                response.Success = false;
                response.Message = "Person not found";
            }

            StringContent restContent = new StringContent( JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json" );
            return new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = restContent
            };
        }
    }
}
