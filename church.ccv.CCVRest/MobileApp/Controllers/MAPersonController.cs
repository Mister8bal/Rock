using System;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using church.ccv.CCVRest.Common.Model;
using church.ccv.CCVRest.MobileApp.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest.Filters;
using Rock.Web.Cache;

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
                MAPersonModel personModel = MAPersonService.GetMobileAppPerson( personId.Value );

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
                MAPersonModel personModel = MAPersonService.GetMobileAppPerson( personAlias.PersonId );

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
        public HttpResponseMessage UpdatePerson( [FromBody] MAPersonModel mobileAppPerson )
        {
            MAPersonService.UpdateMobileAppResult result = MAPersonService.UpdateMobileAppPerson( mobileAppPerson );
            switch ( result )
            {
                case MAPersonService.UpdateMobileAppResult.Success:
                {
                    // Everything worked, and we want to make things easy on the Mobile App, so now
                    // grab the updated person and return it.
                    // (we have to grab them based 
                    int personId = new PersonAliasService( new RockContext() ).Get( mobileAppPerson.PrimaryAliasId ).PersonId;
                    MAPersonModel updatedModel = MAPersonService.GetMobileAppPerson( personId );

                    if ( updatedModel != null )
                    {
                        return Common.Util.GenerateResponse( true, UpdatePersonResponse.Success.ToString(), updatedModel );
                    }
                    else
                    {
                        // somehow we saved the person...but couldn't re-load them? Makes no sense and shouldn't happen,
                        // but if it DOES, tell the Mobile App we failed so they don't overwrite their existing data.
                        return Common.Util.GenerateResponse( false, UpdatePersonResponse.NotSet.ToString(), null );
                    }
                }

                case MAPersonService.UpdateMobileAppResult.PersonNotFound:
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
                if ( MAPersonService.SaveAttendanceRecord( personAlias, campusId, Request.Headers.Host, Request.Headers.UserAgent.ToString( ) ) )
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
                if ( MAPersonService.HasAttendanceRecord( personAlias ) )
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

        [Obsolete("Access Token is now returned in the PersonData API. Remove this after the next Mobile App Upgrade", false)]
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
            PersonNotEligible,
            InvalidModel
        }

        [System.Web.Http.HttpPost]
        [System.Web.Http.Route( "api/NewMobileApp/PersonPhoto" )]
        [Authenticate, Secured]
        public HttpResponseMessage PersonPhoto( [FromBody] PersonPhotoModel personPhoto )
        {
            int? photoId = null;
            Common.Util.UpdatePersonPhotoResult photoResult = Common.Util.UpdatePersonPhoto( personPhoto, out photoId );
            switch( photoResult )
            {
                case Common.Util.UpdatePersonPhotoResult.Success:
                {
                        // on success, provide the mobile app with the updated photoURL
                    string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();
                    string photoURL = publicAppRoot + "GetImage.ashx?Id=" + photoId;
                    return Common.Util.GenerateResponse( true, PersonPhotoResponse.Success.ToString(), photoURL );
                }

                case Common.Util.UpdatePersonPhotoResult.PersonNotFound:
                {
                    return Common.Util.GenerateResponse( false, PersonPhotoResponse.PersonNotFound.ToString(), null );
                }

                case Common.Util.UpdatePersonPhotoResult.PersonNotEligible:
                {
                    return Common.Util.GenerateResponse( false, PersonPhotoResponse.PersonNotEligible.ToString(), null );
                }

                default:
                {
                    return Common.Util.GenerateResponse( false, PersonPhotoResponse.InvalidModel.ToString(), null );
                }
            }
        }
    }
}
