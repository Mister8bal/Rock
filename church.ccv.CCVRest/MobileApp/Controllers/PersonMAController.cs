using System;
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
        [Serializable]
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


        [Serializable]
        public enum AttendanceResponse
        {
            Success,
            PersonNotFound,
            AlreadyAttended
        }

        [System.Web.Http.HttpPost]
        [System.Web.Http.Route( "api/NewMobileApp/Attendance" )]
        [Authenticate, Secured]
        public HttpResponseMessage Attendance( int primaryAliasId, int? campusId )
        {
            RockContext rockContext = new RockContext();

            // get the person ID by their primary alias id
            PersonAliasService paService = new PersonAliasService( rockContext );
            PersonAlias personAlias = paService.Get( primaryAliasId );

            if ( personAlias != null )
            {
                // try saving the attendance record--if it returns true we did, if not they've already marked attendance this weekend.
                if ( MobileAppService.SaveAttendanceRecord( personAlias, campusId, Request.Headers.Host, Request.Headers.UserAgent.ToString( ) ) )
                {
                    return Common.Util.GenerateResponse( true, AttendanceResponse.Success.ToString(), null );
                }
                else
                {
                    return Common.Util.GenerateResponse( false, AttendanceResponse.AlreadyAttended.ToString(), null );
                }
            }

            return Common.Util.GenerateResponse( false, AttendanceResponse.PersonNotFound.ToString(), null );
        }

        [Serializable]
        public enum AttendedResponse
        {
            Attended,
            NotAttended,
            PersonNotFound
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Attended" )]
        [Authenticate, Secured]
        public HttpResponseMessage Attended( int primaryAliasId )
        {
            RockContext rockContext = new RockContext();

            // get the person ID by their primary alias id
            PersonAliasService paService = new PersonAliasService( rockContext );
            PersonAlias personAlias = paService.Get( primaryAliasId );

            if ( personAlias != null )
            {
                if ( MobileAppService.HasAttendanceRecord( personAlias ) )
                {
                    return Common.Util.GenerateResponse( true, AttendedResponse.Attended.ToString(), null );
                }
                else
                {
                    return Common.Util.GenerateResponse( true, AttendedResponse.NotAttended.ToString(), null );
                }
            }

            return Common.Util.GenerateResponse( false, AttendedResponse.PersonNotFound.ToString(), null );
        }
    }
}
