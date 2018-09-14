using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    [DisplayName( "TalntLMS User" )]
    [Category( "CCV > HR" )]
    [Description( "SSO login for Church Online Platform. Add this block to an external facing page and point Church Online Platform at the external page." )]
    [TextField( "TalentLMS API Key", "You can find the API key on the Account & Settings > Basic Settings page in TalentLMS.", true, "", "", 1, "APIKey" )]
    [TextField( "TalentLMS API Url", "Your TalentLMS API Url", true, "", "", 2, "APIUrl" )]

    public partial class TalentLMSUser : Rock.Web.UI.RockBlock
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // Get API key and Url, display error if either imissing
            if ( GetAttributeValue( "APIKey" ).IsNullOrWhiteSpace() || GetAttributeValue( "APIUrl" ).IsNullOrWhiteSpace() )
            {
                DisplayError( "Block not configured: Please update block settings." );
                return;
            }

            // Prep for API calls
            string apiKey = GetAttributeValue( "APIKey" );
            string apiUrl = GetAttributeValue( "APIUrl" ) + "/api/v1/";
            int count = 0;
            string errorMessage = "";
            JArray coursesBlob = null;
            JObject userBlob = null;

        // API call for available courses
        var coursesClient = new RestClient( apiUrl + "courses" );
            coursesClient.Authenticator = new HttpBasicAuthenticator( apiKey, "" );
            var coursesRequest = new RestRequest( Method.GET );
            coursesClient.ExecuteAsync( coursesRequest, response =>
            {
                // check response status
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Success - Assign response content to courses field
                    coursesBlob = JArray.Parse( response.Content );
                }
                else
                {
                    // Failed - pass error message
                    errorMessage = response.Content;
                }

                Interlocked.Increment( ref count );
            });
            
            // API calls for user info
            var userClient = new RestClient( apiUrl + "users/email:" + CurrentUser.Person.Email );
            userClient.Authenticator = new HttpBasicAuthenticator( apiKey, "" );
            var usersRequest = new RestRequest( Method.GET );
            userClient.ExecuteAsync( usersRequest, response =>
            {
                if ( response.StatusCode == System.Net.HttpStatusCode.OK )
                {
                    userBlob = JObject.Parse( response.Content );
                }
                // Process response
                else
                {
                    errorMessage = response.Content;
                }


                Interlocked.Increment( ref count );

            } );

            while ( count != 2 )
            {
                Thread.Sleep( 1 );
            }

            // Check that blob content has been set before proceeding
            if ( coursesBlob == null || userBlob == null )
            {
                // Missing blob content - if errorMessage doesnt already exist set message
                if ( errorMessage.IsNullOrWhiteSpace() ) {
                    errorMessage = "API Content missing.";
                }

                DisplayError( errorMessage );
                return;
            }

            // Render user panel
            RenderUserPanel( userBlob, coursesBlob );

            // Render courses panel
            RenderCoursesPanel( coursesBlob, userBlob );
        }

        private void RenderUserPanel( JObject userBlob, JArray coursesBlob )
        {
            //course["custom-link"] = "<a href=\"google.com\" target=\"_blank\">Hi</a>";
        }

        private void RenderCoursesPanel( JArray jsonBlob, JObject userBlob )
        {
            JArray gridCourses = new JArray();

            // build list of enrolled courses
            JArray enrolledCourses = (JArray)userBlob["courses"];

            foreach ( JObject course in jsonBlob )
            {
                string customLink = "Enroll";
                foreach ( var enrolledCourse in enrolledCourses )
                {
                    // Check if user enrolled in course
                    if ( (string)enrolledCourse["id"] == (string)course["id"] )
                    {
                        customLink = "Drop";
                    }

                    course["custom-link"] = customLink;
                    gridCourses.Add( course );

                }
            }



            var courses = from c in jsonBlob.Children()
                            select new
                            {
                                Name = ( string ) c["name"],
                                Action = (string)c["custom-link"]
                            };





            gGrid.DataSource = courses.ToList();
            gGrid.DataBind();




        }

        private void DisplayError( string message )
        {
            pnlMessage.Controls.Clear();
            pnlMessage.Controls.Add( new LiteralControl( message ) );
            pnlMessage.Visible = true;
        }


        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            // Display User dashboard
        }
    }
}
