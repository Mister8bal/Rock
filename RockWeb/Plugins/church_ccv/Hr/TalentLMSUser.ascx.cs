using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using Rock;
using Rock.Attribute;

namespace RockWeb.Plugins.church_ccv.Hr
{
    #region Block Attributes

    /// <summary>
    /// TalentLMS dashboard that displays all courses and
    /// course info for current user if their email
    /// address matches an email address in TalentLMS
    /// </summary>
    [DisplayName( "TalntLMS User" )]
    [Category( "CCV > HR" )]
    [Description( "SSO login for Church Online Platform. Add this block to an external facing page and point Church Online Platform at the external page. Note: Current User email address must match a user in TalentLMS for block to show" )]
    [TextField( "TalentLMS API Key", "You can find the API key on the Account & Settings > Basic Settings page in TalentLMS.", true, "", "", 1, "APIKey" )]
    [TextField( "TalentLMS Url", "Your TalentLMS Url", true, "", "", 2, "TalentLMSUrl" )]

    #endregion

    public partial class TalentLMSUser : Rock.Web.UI.RockBlock
    {
        #region Fields

        private string _talentLMSUrl = null;
        private string _apiKey = null;
        private string _apiUrl = null;
        private JArray _coursesBlob = null;
        private JObject _userBlob = null;
        private string _userLoginKey = null;
        private List<Course> _userEnrolledCourses = null;

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // Get API key and Url, display error if either imissing
            if ( GetAttributeValue( "APIKey" ).IsNullOrWhiteSpace() || GetAttributeValue( "TalentLMSUrl" ).IsNullOrWhiteSpace() )
            {
                DisplayError( "Block not configured: Please update block settings." );

                return;
            }

            // Prep for API calls
            _apiKey = GetAttributeValue( "APIKey" );
            _talentLMSUrl = GetAttributeValue( "TalentLMSUrl" );
            _apiUrl = _talentLMSUrl + "/api/v1/";            
        }


        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            string errorMessage = String.Empty;
            string allCoursesErrorMessage = String.Empty;

            if ( !Page.IsPostBack )
            {
                // request user
                if ( !RequestUser( out _userBlob ) )
                {
                    // user doesnt exist, hide talentLMS and stop processing
                    pnlTalentLMS.Visible = false;
                    return;
                }
 
                // request all courses from TalentLMS API
                _coursesBlob = RequestAllCourses( out allCoursesErrorMessage );
                
                // check that blob content has been set before proceeding
                if ( _coursesBlob.Count == 0 || _userBlob.Count == 0 )
                {
                    // content missing
                    errorMessage = "Failed to retrieve content from TalentLMS<br /><br />";

                    if ( allCoursesErrorMessage != String.Empty )
                    {
                        errorMessage += allCoursesErrorMessage + "<br /><br />";
                    }

                    DisplayError( errorMessage );

                    return;
                }

                // hydrate class user variables
                string userErrorMessage = String.Empty;
                if ( !HydrateUserInfo( _userBlob, out userErrorMessage ) )
                {
                    // hydrate failed
                    errorMessage = "TalentLMS Error<br /><br />";

                    if ( userErrorMessage != String.Empty )
                    {
                        errorMessage += userErrorMessage;
                    }

                    DisplayError( errorMessage );

                    return;
                }

                // bind user courses
                BindUserCourses( _userEnrolledCourses );

                // bind available courses
                BindAvailableCourses( _coursesBlob );
            }
        }

        #endregion

        #region Events
        /// <summary>
        /// Click event for Enroll Button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnEnroll_Click( object sender, EventArgs e )
        {
            // ensure we have what we need for api post
            if ( _apiUrl != String.Empty && _apiKey != String.Empty && hfUserId.Value != String.Empty )
            {
                Button btnClicked = sender as Button;

                // setup API call
                var enrollClient = new RestClient( _apiUrl + "addusertocourse" );

                enrollClient.Authenticator = new HttpBasicAuthenticator( _apiKey, "" );

                var enrollRequest = new RestRequest( Method.POST );

                enrollRequest.AddParameter( "user_id", hfUserId.Value );
                enrollRequest.AddParameter( "course_id", btnClicked.CommandArgument );
                enrollRequest.AddParameter( "role", "learner" );

                // make API call
                var response = enrollClient.Execute( enrollRequest );
                
                // Force page refresh after API call completes      
                Page.Response.Redirect( Page.Request.Url.ToString(), true );
            } else
            {
                DisplayError( "Missing API settings" );
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Hydrate user variables
        /// </summary>
        /// <param name="userBlob"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        private bool HydrateUserInfo( JObject userBlob, out string errorMessage )
        {
            // parse out user key
            var userLoginKeyArray = _userBlob["login_key"].ToString().Split( ':' );

            if ( userLoginKeyArray.Length > 1 )
            {
                // parse success - assign _userLoginKey, note key is 3rd element in array
                _userLoginKey = userLoginKeyArray[2];
            }

            // assign user id to hidden field
            hfUserId.Value = _userBlob["id"].ToString();

            // check if required variables have values
            if ( _userLoginKey.IsNullOrWhiteSpace() || hfUserId.Value == String.Empty )
            {
                // something failed
                errorMessage = "Failed to poplate user info";
                return false;
            }

            // create _userEnrolledCourses
            var courses = from c in userBlob["courses"].Children()
                          select new Course
                          {
                              Name = CreateCourseName( ( string ) c["name"], ( string ) ( c["expired_on"] ) ),
                              Id = ( string ) c["id"],
                              Progress = CreateCourseStatusBadge( ( string ) c["completion_status"], Int32.Parse( ( string ) c["completion_percentage"] ) ),
                              LaunchButton = CreateCourseLaunchButton( ( string ) c["id"] )
                          };

            _userEnrolledCourses = courses.ToList();

            // success
            errorMessage = "";
            return true;
        }

        /// <summary>
        /// Request user from TalentLMS api
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        private bool RequestUser( out JObject userBlob )
        {
            bool userInTalentLMS = false;

            userBlob = new JObject();

            // setup API call
            var userClient = new RestClient( _apiUrl + "users/email:" + CurrentUser.Person.Email );
            userClient.Authenticator = new HttpBasicAuthenticator( _apiKey, "" );

            var usersRequest = new RestRequest( Method.GET );

            // make API call
            var response = userClient.Execute( usersRequest );

            if ( response.StatusCode == System.Net.HttpStatusCode.OK )
            {
                // success
                userInTalentLMS = true;
                // assign response content to userBlob field
                userBlob = JObject.Parse( response.Content );
            }

            return userInTalentLMS;
        }

        /// <summary>
        /// Request all courses from TalentLMS api
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        private JArray RequestAllCourses( out string errorMessage )
        {
            JArray allCourses = new JArray();
            errorMessage = String.Empty;

            // setup API call
            var coursesClient = new RestClient( _apiUrl + "courses" );
            coursesClient.Authenticator = new HttpBasicAuthenticator( _apiKey, "" );

            var coursesRequest = new RestRequest( Method.GET );

            // make API call
            var response = coursesClient.Execute( coursesRequest );

            // check response status
            if ( response.StatusCode == System.Net.HttpStatusCode.OK )
            {
                // success - Assign response content to coursesBlob field
                allCourses = JArray.Parse( response.Content );
            }
            else
            {
                // failed - pass error message
                errorMessage = response.Content;
            }

            return allCourses;
        }


        /// <summary>
        /// Bind the User Courses
        /// </summary>
        private void BindUserCourses( List<Course> courses)
        {
            // bind list to user grid
            rptUserCourses.DataSource = courses;
            rptUserCourses.DataBind();
        }

        /// <summary>
        /// Render the all courses talentlms panel
        /// </summary>
        private void BindAvailableCourses( JArray courses )
        {
            // create new list
            List<Course> availableCourses = new List<Course>();

            // get all courses from blob
            var allCourses = from c in courses.Children()
                            select new Course
                            {
                                Name = ( string ) c["name"],
                                Description = ( string ) c["description"],
                                Id = ( string ) c["id"],
                                HideFromCatalog = ( string ) c["hide_from_catalog"]
                            };

            foreach ( var course in allCourses )
            {
                // add course to list if not enrolled and course not hidden from catalog
                if ( !_userEnrolledCourses.Any( i => i.Id == course.Id ) && course.HideFromCatalog != "1" )
                {
                    availableCourses.Add( course );
                }
            }

            // bind list
            rptAllCourses.DataSource = availableCourses;
            rptAllCourses.DataBind();
        }



        /// <summary>
        /// Display an error message
        /// </summary>
        /// <param name="message"></param>
        private void DisplayError( string message )
        {
            // hide talentLMS dashboard
            pnlTalentLMSDashboard.Visible = false;

            // dislay the message
            pnlMessage.Controls.Clear();
            pnlMessage.Controls.Add( new LiteralControl( message ) );
            pnlMessage.Visible = true;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Create an html course name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="expires"></param>
        /// <returns></returns>
        private string CreateCourseName( string name, string expiration )
        {
            if ( expiration.IsNullOrWhiteSpace() )
            {
                // course has no expiration date - return just the name
                return String.Format( "<div class=\"course-name\">{0}</div>", name );
            }
            else
            {
                // course has expiration date - convert date string into DateTime object
                DateTime expirationDate = DateTime.ParseExact( expiration, "dd/MM/yyyy, HH:mm:ss", CultureInfo.InvariantCulture );

                // set default expiration text
                string expirationText = "expires";

                // check if expiration date is in past
                if ( expirationDate < DateTime.Now )
                {
                    // in past - change expiration text
                    expirationText = "expired";
                }

                return String.Format( "<div class=\"course-name\">{0}<span class=\"{1}\">{1} on {2}</span></div>", name, expirationText, expirationDate.ToString( "MM/dd/yyyy" ) );
            }
        }

        /// <summary>
        /// Create an html course status badge
        /// </summary>
        /// <param name="status"></param>
        /// <param name="percentage"></param>
        /// <returns></returns>
        private string CreateCourseStatusBadge( string status, int percentage )
        {
            if ( status == "completed" )
            {
                // course complete
                return "<span class=\"progress completed\">Completed</span>";
            }
            else
            {
                // course not complete
                string cssClass = "progress";

                if ( percentage == 0 )
                {
                    cssClass += " zero";
                }

                return String.Format( "<div class=\"{0}\"><div class=\"progress-bar\" role=\"progressbar\" aria-valuenow=\"{1}\" aria-valuemin=\"0\" aria-valuemax=\"100\" style=\"width:{1}%\">{1}%</div></div>", cssClass, percentage );
            }
        }

        /// <summary>
        /// Create an html button to launch a course
        /// </summary>
        /// <param name="courseId"></param>
        /// <returns></returns>
        private string CreateCourseLaunchButton( string courseId )
        {
            return String.Format( "<a href=\"{0}/index/gotocourse/key:{1},course_id:{2}\" target=\"_blank\" class=\"btn btn-primary btn-talentlms\">Launch</a></br>", _talentLMSUrl, _userLoginKey, courseId );
        }

        #endregion

        #region Helper Classes
        protected class Course
        {
            public string Name { get; set; }
            public string Id { get; set; }
            public string Description { get; set; }
            public string Progress { get; set; }
            public string LaunchButton { get; set; }
            public string HideFromCatalog { get; set; }

            public Course()
            {
            }
        }

        #endregion
    }
}
