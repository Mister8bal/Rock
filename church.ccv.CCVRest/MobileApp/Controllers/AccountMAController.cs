using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using church.ccv.CCVRest.MobileApp.Model;
using Rock.Data;
using Rock.Model;
using Rock.Rest.Filters;
using Rock.Security;

namespace church.ccv.CCVRest.MobileApp
{
    public partial class NewMobileAppController : Rock.Rest.ApiControllerBase
    {
        [Serializable]
        public enum LoginResponse
        {
            Success,

            InvalidModel,
            InvalidLoginType,

            InvalidUsername,
            InvalidPassword,

            AccountUnconfirmed
        }

        [System.Web.Http.HttpPost]
        [System.Web.Http.Route( "api/NewMobileApp/Login" )]
        [Authenticate, Secured]
        public HttpResponseMessage Login( [FromBody]LoginModel loginModel )
        {
            RockContext rockContext = new RockContext();

            // require login parameters
            if ( loginModel == null )
            {
                return Common.Util.GenerateResponse( false, LoginResponse.InvalidModel.ToString( ), null );
            }


            // verify their user login
            var userLoginService = new UserLoginService( rockContext );
            var userLogin = userLoginService.GetByUserName( loginModel.Username );
            if ( userLogin == null || userLogin.EntityType == null )
            {
                return Common.Util.GenerateResponse( false, LoginResponse.InvalidUsername.ToString( ), null );
            }

            // verify their password
            var component = AuthenticationContainer.GetComponent( userLogin.EntityType.Name );
            if ( component == null || component.IsActive == false )
            {
                return Common.Util.GenerateResponse( false, LoginResponse.InvalidLoginType.ToString( ), null );
            }

            if ( component.Authenticate( userLogin, loginModel.Password ) == false )
            {
                return Common.Util.GenerateResponse( false, LoginResponse.InvalidPassword.ToString( ), null );
            }


            // ensure there's a person associated with this login.
            if ( userLogin.PersonId.HasValue == false )
            {
                return Common.Util.GenerateResponse( false, LoginResponse.InvalidLoginType.ToString( ), null );
            }


            // if this account hasn't been confirmed yet, do not let them login. This prevents someone from
            // trying to do a password reset on a stolen email address.
            if ( userLogin.IsConfirmed == false )
            {
                return Common.Util.GenerateResponse( false, LoginResponse.AccountUnconfirmed.ToString( ), null );
            }

            return Common.Util.GenerateResponse( true, LoginResponse.Success.ToString( ), null );
        }


        [Serializable]
        public enum RegisterNewUserResponse
        {
            Success,

            InvalidModel,
            CreationError,

            UsernameAlreadyExists,

            PersonHasUsername,
            PersonAlreadyExists
        }

        [System.Web.Http.HttpPost]
        [System.Web.Http.Route( "api/NewMobileApp/RegisterNewUser" )]
        [Authenticate, Secured]
        public HttpResponseMessage RegisterNewUser( [FromBody]NewUserModel newUserModel )
        {
            if ( newUserModel == null )
            {
                return Common.Util.GenerateResponse( false, RegisterNewUserResponse.InvalidModel.ToString( ), null );
            }

            RockContext rockContext = new RockContext();
            UserLoginService userLoginService = new UserLoginService( rockContext );

            // first, if the user name already exists, simply stop and tell them the username already exists.
            UserLogin userLogin = userLoginService.GetByUserName( newUserModel.Username );
            if ( userLogin != null )
            {
                return Common.Util.GenerateResponse( false, RegisterNewUserResponse.UsernameAlreadyExists.ToString( ), null );
            }

            // Now make sure the person doesn't already exist
            PersonService personService = new PersonService( rockContext );
            IEnumerable<Person> personList = personService.GetByMatch( newUserModel.FirstName, newUserModel.LastName, newUserModel.Email );
            if ( personList.Count() > 0 )
            {
                // the person exists, so now see if they already have a username, too
                bool personHasUsername = false;
                foreach ( var person in personList )
                {
                    var loginList = userLoginService.GetByPersonId( person.Id );
                    if ( loginList.Count() > 0 )
                    {
                        personHasUsername = true;
                        break;
                    }
                }

                // the person exists AND has a username, so say that
                if ( personHasUsername == true )
                {
                    return Common.Util.GenerateResponse( false, RegisterNewUserResponse.PersonHasUsername.ToString( ), null );
                }

                // otherwise let them know the person already exists
                return Common.Util.GenerateResponse( false, RegisterNewUserResponse.PersonAlreadyExists.ToString( ), null );
            }


            // we know we can create the person. So first, begin tracking who made these changes, and then
            // create the person with their login
            if ( MobileAppService.RegisterNewPerson( newUserModel ) == false )
            {
                return Common.Util.GenerateResponse( false, RegisterNewUserResponse.CreationError.ToString( ), null );
            }

            // everything went ok!
            return Common.Util.GenerateResponse( true, RegisterNewUserResponse.Success.ToString( ), null );
        }
    }
}
