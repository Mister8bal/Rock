using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json.Linq;
using Rock.Attribute;
using Rock.Model;
using Rock.Security;

namespace church.ccv.Authentication
{
    /// <summary>
    /// Authenticates a username/password using Azure Active Directory. 
    /// NOTE: This is not a supported authentication scenario by Microsoft and is advised against using... 
    /// Microsoft recommends/requires redirect to the Azure Login Page for authentication.
    /// This authentication provider was created to mimic the Active Directory on premise provider using Azure Active Directory.
    /// You have been warned, use at your own risk!
    /// </summary>
    [Description("Azure Active Directory Authentication Provider")]
    [Export(typeof(AuthenticationComponent))]
    [ExportMetadata("ComponentName", "Azure Active Directory")]
    [TextField( "Azure Active Directory Tenant Id", "The Id of your Azure Active Directory Tenant", true, "", "Azure", 1 )]
    [TextField( "Azure Active Directory Client Application Id", "The Id of your Azure Active Directory Client Application", true, "", "Azure", 2 )]
    [TextField( "Azure Active Directory Client Secret", "The client secret for your Azure Active Directory Client Application", true, "", "Azure", 3 )]
    public class AzureActiveDirectory : AuthenticationComponent
    {
        /// <summary>
        /// Gets the type of the service.
        /// </summary>
        /// <value>
        /// The type of the service.
        /// </value>
        public override AuthenticationServiceType ServiceType
        {
            get { return AuthenticationServiceType.External; }
        }

        /// <summary>
        /// Determines if user is directed to another site (i.e. Facebook, Gmail, Twitter, etc) to confirm approval of using
        /// that site's credentials for authentication.
        /// </summary>
        /// <value>
        /// The requires remote authentication.
        /// </value>
        public override bool RequiresRemoteAuthentication
        {
            get { return false; }
        }

        /// <summary>
        /// Authenticates the specified user name and password
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        public override bool Authenticate(UserLogin user, string password)
        {
            // Get attribute values
            string aadTenantId = GetAttributeValue( "AzureActiveDirectoryTenantId" );
            string aadClientApplicationId = GetAttributeValue( "AzureActiveDirectoryClientApplicationId" );
            string aadClientSecret = GetAttributeValue( "AzureActiveDirectoryClientSecret" );

           // Check for azure configuration settings
            if ( !String.IsNullOrWhiteSpace( aadTenantId ) || !String.IsNullOrWhiteSpace( aadClientApplicationId ) || !String.IsNullOrWhiteSpace( aadClientSecret ) )
            {
                // Azure Active Directory OAuth Url
                string oAuthUrl = String.Format( "https://login.microsoftonline.com/{0}/oauth2/token", aadTenantId );

                // URL encoded strings for body of post
                string clientIdUrlEncoded = HttpUtility.UrlEncode( aadClientApplicationId );
                string grantTypeUrlEncoded = HttpUtility.UrlEncode( "password" );
                string usernameUrlEncoded = HttpUtility.UrlEncode( user.UserName );
                string passwordUrlEncoded = HttpUtility.UrlEncode( password );
                string clientSecretUrlEncoded = HttpUtility.UrlEncode( aadClientSecret );

                // build the body string, and encode body
                string body = String.Format( "resource={0}&client_id={0}&grant_type={1}&username={2}&password={3}&client_secret={4}", clientIdUrlEncoded, grantTypeUrlEncoded, user.UserName, passwordUrlEncoded, clientSecretUrlEncoded );
                var encodedBody = Encoding.ASCII.GetBytes( body );
                
                // Create the web request
                WebRequest request = HttpWebRequest.Create( oAuthUrl );

                // Configure the request
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = encodedBody.Length;

                // add the body to the request
                Stream dataStream = request.GetRequestStream();
                dataStream.Write( encodedBody, 0, encodedBody.Length );
                dataStream.Close();

                try
                {
                    // Make the request
                    WebResponse response = request.GetResponse();

                    // Get the response and convert to json object
                    Stream responseData = response.GetResponseStream();
                    StreamReader readerData = new StreamReader( responseData );
                    string responseFromServer = readerData.ReadToEnd();
                    var jobject = JObject.Parse( responseFromServer )["access_token"];

                    response.Close();
                    // Check for access token
                    // if null return false - aka failed authentication
                    // if token exists return true - aka successful authentication
                    return !String.IsNullOrWhiteSpace( jobject.ToString() );              

                } 
                catch
                {
                    // A non 200 reposnse was return indicating something failed
                    // return false - aka failed authentication

                    return false;
                }
            }
            else
            {
                // Missing azure configuration settings, return false - aka failed authentiation
                return false;
            }
        }

        /// <summary>
        /// Encodes the password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password"></param>
        /// <returns></returns>
        public override string EncodePassword(UserLogin user, string password)
        {
            return null;
        }

        /// <summary>
        /// Authenticates the user based on a request from a third-party provider.  Will set the username and returnUrl values.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override bool Authenticate(System.Web.HttpRequest request, out string userName, out string returnUrl)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Generates the login URL.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override Uri GenerateLoginUrl(System.Web.HttpRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tests the Http Request to determine if authentication should be tested by this
        /// authentication provider.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override bool IsReturningFromAuthentication(System.Web.HttpRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the URL of an image that should be displayed.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override string ImageUrl()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a value indicating whether [supports change password].
        /// </summary>
        /// <value>
        /// <c>true</c> if [supports change password]; otherwise, <c>false</c>.
        /// </value>
        public override bool SupportsChangePassword
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Changes the password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="oldPassword">The old password.</param>
        /// <param name="newPassword">The new password.</param>
        /// <param name="warningMessage">The warning message.</param>
        /// <returns></returns>
        public override bool ChangePassword( UserLogin user, string oldPassword, string newPassword, out string warningMessage )
        {
            warningMessage = "not supported";
            return false;
        }

        /// <summary>
        /// Sets the password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void SetPassword(UserLogin user, string password)
        {
            throw new NotImplementedException();
        }
    }
}