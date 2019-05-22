
using System;
using System.ComponentModel;
using Rock.Model;
using Rock.Web.UI;
using Rock.Data;
using church.ccv.PersonalizationEngine.Model;
using Rock.Web.UI.Controls;
using Rock;
using Rock.Security;
using System.Linq;
using System.Data.Entity;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using Rock.Attribute;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Web.UI;
using church.ccv.PersonalizationEngine.Data;
using System.Text.RegularExpressions;

namespace RockWeb.Plugins.church_ccv.Groups
{
    [DisplayName( "MobileApp AP Board" )]
    [Category( "CCV > Groups" )]
    [Description( "Displays the board where an AP can post content." )]
    public partial class MobileAppAPBoard : RockBlock
    {
        // Dev Id
        const int ContentChannelId_ToolboxAPBoard = 295;

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                BindAPBoardItem( );
            }
        }

        protected void BindAPBoardItem( )
        {
            bool success = false;

            // populate the UI with the ap board data
            int? contentChannelItemId = PageParameter( "contentItemId" ).AsIntegerOrNull();

            // make sure a content channel item id was specified
            if ( contentChannelItemId > 0 )
            {
                // grab this content channel item
                RockContext rockContext = new RockContext();

                ContentChannelService contentChannelService = new ContentChannelService( rockContext );
                ContentChannel apBoard = contentChannelService.Get( ContentChannelId_ToolboxAPBoard );

                var apBoardItem = apBoard.Items.Where( i => i.Id == contentChannelItemId ).SingleOrDefault();
                if ( apBoardItem != null )
                {
                    apBoardItem.LoadAttributes();

                    lTitle.Text = "<h4 class=\"panel-title\">" + "AP Board: " + apBoardItem.Title + "</h4>";
                    tbContent.Text = apBoardItem.Content;
                    tbTipOfTheWeek.Text = apBoardItem.AttributeValues["TipOfTheWeek"].ToString();

                    success = true;
                }
            }


            if ( success == true )
            {
                upnlSettings.Visible = true;
                errorPanel.Visible = false;
            }
            else
            {
                upnlSettings.Visible = false;
                errorPanel.Visible = true;
            }
        }
        
        protected void btnSave_Click( object sender, EventArgs e )
        {
            // populate the UI with the ap board data
            int? contentChannelItemId = PageParameter( "contentItemId" ).AsIntegerOrNull();

            // make sure a content channel item id was specified
            if ( contentChannelItemId > 0 )
            {
                RockContext rockContext = new RockContext();
                ContentChannelService contentChannelService = new ContentChannelService( rockContext );
                ContentChannel apBoard = contentChannelService.Get( ContentChannelId_ToolboxAPBoard );

                var apBoardItem = apBoard.Items.Where( i => i.Id == contentChannelItemId ).SingleOrDefault();
                if ( apBoardItem != null )
                {
                    apBoardItem.LoadAttributes();

                    apBoardItem.Content = tbContent.Text;
                    apBoardItem.AttributeValues["TipOfTheWeek"].Value = tbTipOfTheWeek.Text;
                    apBoardItem.StartDateTime = DateTime.Now.Date;

                    apBoardItem.SaveAttributeValues( rockContext );
                    rockContext.SaveChanges();

                    RefreshCurrentPage( contentChannelItemId.Value );
                }
            }
        }

        #region Utility
        void RefreshCurrentPage( int personaId )
        {
            // refresh the current page
            var queryParams = new Dictionary<string, string>( );
            queryParams.Add( "contentItemId", personaId.ToString( ) );
            NavigateToCurrentPage( queryParams );
        }
        #endregion
    }
}