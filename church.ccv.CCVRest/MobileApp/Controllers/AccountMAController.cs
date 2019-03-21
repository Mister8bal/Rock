using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using church.ccv.CCVRest.MobileApp.Model;
using Rock;
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
        public enum FacebookLoginResponse
        {
            Success,

            InvalidModel,

            Failed
        }

        [HttpPost]
        [System.Web.Http.Route( "api/NewMobileApp/FacebookLogin" )]
        [Authenticate, Secured]
        public HttpResponseMessage FacebookLogin( [FromBody]Rock.Security.ExternalAuthentication.Facebook.FacebookUser facebookUser )
        {
            if ( facebookUser == null )
            {
                return Common.Util.GenerateResponse( false, FacebookLoginResponse.InvalidModel.ToString(), null );
            }

            // Rock associates Facebook by creating a Login for the person where the "Username" is
            // "FACEBOOK_FACEBOOKID" Ex: FACEBOOK_112383238
            //
            // Given that, GetFacebookUserName  will attempt 3 things:
            // 1. Find a person in Rock with this Facebook ID as a username
            // 2. Find a person in Rock whose First Name, Last Name and Email match what's in facebookUser. It will then create a new FACEBOOK_ID username for them.
            // 3. Create a person in Rock using the facebookUser data, and then attach a new FACEBOOK_ID username to them.
            string userName = Rock.Security.ExternalAuthentication.Facebook.GetFacebookUserName( facebookUser );
            if ( !string.IsNullOrWhiteSpace( userName ) && userName.Length > "FACEBOOK_".Length )
            {
                // on success, pass back success and the username so the caller knows what to use to reference this person in the future.
                return Common.Util.GenerateResponse( true, FacebookLoginResponse.Success.ToString( ), userName );
            }
            else
            {
                return Common.Util.GenerateResponse( false, FacebookLoginResponse.Failed.ToString(), null );
            }
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


        [Serializable]
        public enum ForgotPasswordResponse
        {
            Success,
            EmailNotFound,
            InvalidEmail
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/ForgotPassword" )]
        [Authenticate, Secured]
        public HttpResponseMessage ForgotPassword( string emailAddress )
        {
            // Ignore blank  or invalid email addresses
            if ( string.IsNullOrWhiteSpace( emailAddress ) == true || emailAddress.IsValidEmail( ) == false )
            {
                return Common.Util.GenerateResponse( false, ForgotPasswordResponse.InvalidEmail.ToString(), null );
            }

            // try building and sending the email--if it isn't found, this will return false
            if ( MobileAppService.SendForgotPasswordEmail( emailAddress ) )
            {
                return Common.Util.GenerateResponse( true, ForgotPasswordResponse.Success.ToString(), null );
            }

            // notify them this email wasn't attached to any logins
            return Common.Util.GenerateResponse( false, ForgotPasswordResponse.EmailNotFound.ToString(), null );
        }
    }
}
