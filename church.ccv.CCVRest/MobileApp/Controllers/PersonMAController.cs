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
        public enum RecordAttendanceResponse
        {
            Success,
            PersonNotFound,
            AlreadyAttended
        }

        [System.Web.Http.HttpPost]
        [System.Web.Http.Route( "api/NewMobileApp/Attendance" )] //REMOVE AFTER MA PULL REQUEST IS ACCEPTED
        [System.Web.Http.Route( "api/NewMobileApp/RecordAttendance" )]
        [Authenticate, Secured]
        public HttpResponseMessage RecordAttendance( int primaryAliasId, int? campusId = null )
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
                    return Common.Util.GenerateResponse( true, RecordAttendanceResponse.Success.ToString(), null );
                }
                else
                {
                    return Common.Util.GenerateResponse( false, RecordAttendanceResponse.AlreadyAttended.ToString(), null );
                }
            }

            return Common.Util.GenerateResponse( false, RecordAttendanceResponse.PersonNotFound.ToString(), null );
        }

        [Serializable]
        public enum CheckAttendanceResponse
        {
            Attended,
            NotAttended,
            PersonNotFound
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Attended" )] //REMOVE AFTER MA PULL REQUEST IS ACCEPTED
        [System.Web.Http.Route( "api/NewMobileApp/CheckAttendance" )]
        [Authenticate, Secured]
        public HttpResponseMessage CheckAttendance( int primaryAliasId )
        {
            RockContext rockContext = new RockContext();

            // get the person ID by their primary alias id
            PersonAliasService paService = new PersonAliasService( rockContext );
            PersonAlias personAlias = paService.Get( primaryAliasId );

            if ( personAlias != null )
            {
                if ( MobileAppService.HasAttendanceRecord( personAlias ) )
                {
                    return Common.Util.GenerateResponse( true, CheckAttendanceResponse.Attended.ToString(), null );
                }
                else
                {
                    return Common.Util.GenerateResponse( true, CheckAttendanceResponse.NotAttended.ToString(), null );
                }
            }

            return Common.Util.GenerateResponse( false, CheckAttendanceResponse.PersonNotFound.ToString(), null );
        }
    }
}
