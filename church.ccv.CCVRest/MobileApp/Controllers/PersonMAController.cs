using System.Linq;
using System.Net.Http;
using church.ccv.CCVRest.Common.Model;
using church.ccv.CCVRest.MobileApp.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest.Filters;

namespace church.ccv.CCVRest.MobileApp
{
    public partial class NewMobileAppController : Rock.Rest.ApiControllerBase
    {
        public enum PersonResponse
        {
            Success,
            PersonNotFound
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Person" )]
        [Authenticate, Secured]
        public HttpResponseMessage Person( string userID )
        {
            RockContext rockContext = new RockContext();
            PersonService personService = new PersonService( rockContext );
            UserLoginService userLoginService = new UserLoginService( rockContext );

            // get the person ID by their username
            int? personId = userLoginService.Queryable()
                .Where( u => u.UserName.Equals( userID ) )
                .Select( a => a.PersonId )
                .FirstOrDefault();

            if ( personId.HasValue )
            {
                MobileAppPersonModel personModel = MobileAppService.GetMobileAppPerson( personId.Value );

                return Common.Util.GenerateResponse( true, PersonResponse.Success.ToString( ), personModel );
            }

            return Common.Util.GenerateResponse( false, PersonResponse.PersonNotFound.ToString( ), null );
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Person" )]
        [Authenticate, Secured]
        public HttpResponseMessage Person( int primaryAliasId )
        {
            RockContext rockContext = new RockContext();

            // get the person ID by their primary alias id
            PersonAliasService paService = new PersonAliasService( rockContext );
            PersonAlias personAlias = paService.Get( primaryAliasId );

            if ( personAlias != null )
            {
                MobileAppPersonModel personModel = MobileAppService.GetMobileAppPerson( personAlias.PersonId );

                return Common.Util.GenerateResponse( true, PersonResponse.Success.ToString( ), personModel );
            }

            return Common.Util.GenerateResponse( false, PersonResponse.PersonNotFound.ToString( ), null );
        }
    }
}
