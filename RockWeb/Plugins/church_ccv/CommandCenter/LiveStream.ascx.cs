using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.church_ccv.CommandCenter
{  
    [DisplayName("Live Stream")]
    [Category( "CCV > Command Center" )]
    [Description("Used for viewing live venue streams.")]

    [CampusesField("Campus", "Only shows streams from selected campuses. If none are selected, all campuses will be shown.", false, "", "", 0)]
    [TextField("Venue", "Only shows streams for a specfic venue.", false, order: 1)]
    [CustomDropdownListField( "Screens Per Row", "The number of screens to have per row.", "1,2,3,4", false, "3", Order = 2 )]
    public partial class LiveStream : RockBlock
    {
        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            this.BlockUpdated += Block_BlockUpdated;

            RockPage.AddScriptLink( "~/Plugins/church_ccv/CommandCenter/Assets/video.js" );
            RockPage.AddCSSLink( "~/Plugins/church_ccv/CommandCenter/Assets/video-js.min.css" );
            RockPage.AddCSSLink( "~/Plugins/church_ccv/CommandCenter/Styles/commandcenter.css" );
        }

        /// <summary>
        /// Handles the Load event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            rptvideostreams.DataSource = GetDatasource();
            rptvideostreams.DataBind();

            if ( rptvideostreams.Items.Count < 1)
            {
                ntbAlert.Visible = true;
            }
        }

        /// <summary>
        /// Handles the BlockUpdated event of the Block control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            rptvideostreams.DataSource = GetDatasource();
            rptvideostreams.DataBind();
        }

        #endregion

        /// <summary>
        /// Gets the datasource.
        /// </summary>
        /// <returns></returns>
        private List<VideoData> GetDatasource()
        {
            string configuredVenue = GetAttributeValue( "Venue" );
            string campusGuids = GetAttributeValue( "Campus" );

            var datasource = new List<VideoData>();

            var campuses = CampusCache.All();
            campuses = campuses
                .Where( c => c.IsActive == true )
                .Where( c => campusGuids.Contains( c.Guid.ToString() ) || String.IsNullOrWhiteSpace( campusGuids ) )
                .ToList();

            int uniqueVideoId = 0;

            foreach ( var campus in campuses )
            {
                uniqueVideoId += 1;
                var data = new VideoData();

                var venueList = campus.GetAttributeValue( "VenueStreams" ).ToKeyValuePairList(); /* Key = Venue, Value = Url */
                var venueUrl = venueList.Where( kv => kv.Key == configuredVenue ).Select( kv => kv.Value.ToString() ).FirstOrDefault();

                // if the campus does not have a video for this venue, skip it.
                if ( venueUrl.IsNullOrWhiteSpace() )
                {
                    continue;
                }

                data.VideoId = campus.Name.RemoveSpaces() + "-" + configuredVenue + "-" + uniqueVideoId.ToString();
                data.Campus = campus.Name;
                data.Url = venueUrl;
                data.BoostrapColumn = GetBoostrapColumnClass( GetAttributeValue( "ScreensPerRow" ).AsIntegerOrNull() );
                data.Order = uniqueVideoId;

                datasource.Add( data );
            }

            return datasource;
        }

        /// <summary>
        /// Converts a column count into a bootstrap column class.
        /// </summary>
        /// <param name="columnsPerRow"></param>
        /// <returns></returns>
        private string GetBoostrapColumnClass( int? columnsPerRow )
        {
            if ( columnsPerRow == 1 )
            {
                return "col-md-12";
            }
            else if ( columnsPerRow == 2 )
            {
                return "col-md-6";
            }
            else if ( columnsPerRow == 4 )
            {
                return "col-md-3";
            }
            else
            {
                return "col-md-4";
            }
        }

        /// <summary>
        /// The Video Data.
        /// </summary>
        public class VideoData
        {
            public string VideoId { get; set; }
            public string Campus { get; set; }
            public string Url { get; set; }
            public string BoostrapColumn { get; set; }
            public int Order { get; set; }
        }
    }
}