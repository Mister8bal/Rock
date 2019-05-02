﻿using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
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
            NotSet = -1,
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
        public enum UpdatePersonResponse
        {
            NotSet = -1,
            Success,
            PersonNotFound,
            InvalidModel
        }

        [System.Web.Http.HttpPost]
        [System.Web.Http.Route( "api/NewMobileApp/UpdatePerson" )]
        [Authenticate, Secured]
        public HttpResponseMessage UpdatePerson( [FromBody] MobileAppPersonModel mobileAppPerson )
        {
            MobileAppService.UpdateMobileAppResult result = MobileAppService.UpdateMobileAppPerson( mobileAppPerson );
            switch ( result )
            {
                case MobileAppService.UpdateMobileAppResult.Success:
                {
                    return Common.Util.GenerateResponse( true, UpdatePersonResponse.Success.ToString(), null );
                }

                case MobileAppService.UpdateMobileAppResult.PersonNotFound:
                {
                    return Common.Util.GenerateResponse( false, UpdatePersonResponse.PersonNotFound.ToString(), null );
                }

                default:
                {
                    return Common.Util.GenerateResponse( false, UpdatePersonResponse.InvalidModel.ToString(), null );
                }
            }
        }

        [Serializable]
        public enum RecordAttendanceResponse
        {
            NotSet = -1,
            Success,
            PersonNotFound,
            AlreadyAttended
        }

        [System.Web.Http.HttpPost]
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
            NotSet = -1,
            Attended,
            NotAttended,
            PersonNotFound
        }

        [System.Web.Http.HttpGet]
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

        [Serializable]
        public enum AccessTokenResponse
        {
            NotSet = -1,
            Success,
            PersonNotFound
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/AccessToken" )]
        [Authenticate, Secured]
        public HttpResponseMessage AccessToken( int primaryAliasId )
        {
            RockContext rockContext = new RockContext();

            // get the person ID by their primary alias id
            PersonAliasService paService = new PersonAliasService( rockContext );
            PersonAlias personAlias = paService.Get( primaryAliasId );

            if ( personAlias != null )
            {
                return Common.Util.GenerateResponse( true, AccessTokenResponse.Success.ToString(), "rckipid=" + personAlias.Person.GetImpersonationToken() );
            }

            return Common.Util.GenerateResponse( false, CheckAttendanceResponse.PersonNotFound.ToString(), null );
        }


        [Serializable]
        public enum PersonPhotoResponse
        {
            NotSet = -1,
            Success,
            PersonNotFound,
            InvalidModel
        }

        [System.Web.Http.HttpPost]
        [System.Web.Http.Route( "api/NewMobileApp/PersonPhoto" )]
        [Authenticate, Secured]
        public HttpResponseMessage PersonPhoto( [FromBody] PersonPhotoModel personPhoto )
        {
            Common.Util.UpdatePersonPhotoResult photoResult = Common.Util.UpdatePersonPhoto( personPhoto );
            switch( photoResult )
            {
                case Common.Util.UpdatePersonPhotoResult.Success:
                    return Common.Util.GenerateResponse( true, PersonPhotoResponse.Success.ToString(), null );

                case Common.Util.UpdatePersonPhotoResult.PersonNotFound:
                    return Common.Util.GenerateResponse( false, PersonPhotoResponse.PersonNotFound.ToString(), null );

                default:
                    return Common.Util.GenerateResponse( false, PersonPhotoResponse.InvalidModel.ToString(), null );
            }
        }
    }
}
