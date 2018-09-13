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

            // Get API key, display error if missing
            if ( GetAttributeValue( "APIKey" ).IsNullOrWhiteSpace() || GetAttributeValue( "APIUrl" ).IsNullOrWhiteSpace() )
            {
                DisplayError( "Missing Block Settings" );
                return;
            }

            // Make API calls
            string apiKey = GetAttributeValue( "APIKey" );
            string apiUrl = GetAttributeValue( "APIUrl" ) + "/api/v1/";
            int count = 0;
            bool coursesSuccessful = false;
            bool userSuccessful = false;
            string errorMessage = "";

            // API call for available courses
            var coursesClient = new RestClient( apiUrl + "courses" );
            coursesClient.Authenticator = new HttpBasicAuthenticator( apiKey, "" );
            var coursesRequest = new RestRequest( Method.GET );
            coursesClient.ExecuteAsync( coursesRequest, response =>
            {
                // Check for successful response
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    JArray responseBlob = JArray.Parse( response.Content );

                    RenderCoursesPanel( responseBlob );

                    coursesSuccessful = true;
                }
                // Process response 
                else
                {
                    errorMessage = response.Content;

                }

                Interlocked.Increment( ref count );
            } );
            
            // API calls for user info
            var userClient = new RestClient( apiUrl + "users/email:" + CurrentUser.Person.Email );
            userClient.Authenticator = new HttpBasicAuthenticator( apiKey, "" );
            var usersRequest = new RestRequest( Method.GET );
            userClient.ExecuteAsync( usersRequest, response =>
             {
                 if ( response.StatusCode == System.Net.HttpStatusCode.OK )
                 {
                     JObject responseBlob = JObject.Parse( response.Content );


                     userSuccessful = true;
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

            if ( !coursesSuccessful || !userSuccessful )
            {
                if ( errorMessage.IsNullOrWhiteSpace() ) {
                    errorMessage = "Something failed";
                }

                DisplayError( errorMessage );
                return;
            }

            DisplayError( "It worky" );

        }

        private void RenderCoursesPanel( JArray jsonBlob )
        {
            pnlAllCourses.Controls.Clear();

            Table coursesTable = new Table();
            TableHeaderRow headerRow = new TableHeaderRow();

            coursesTable.Rows.Add( headerRow );

            headerRow.Cells.Add( new TableHeaderCell { Text = "Course Name" } );
            headerRow.Cells.Add( new TableHeaderCell { Text = "Id(button soon)" } );

            coursesTable.Rows.Add( headerRow );

            foreach ( var course in jsonBlob.Children() )
            {
                TableRow row = new TableRow();

                row.Cells.Add( new TableCell { Text = (string)course["name"] } );
                row.Cells.Add( new TableCell { Text = ( string ) course["id"] } );

                coursesTable.Rows.Add( row );

//                pnlAllCourses.Controls.Add( new LiteralControl( ( string ) course["name"] + "<br />" ) );

            }

            pnlAllCourses.Controls.Add( coursesTable );

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
