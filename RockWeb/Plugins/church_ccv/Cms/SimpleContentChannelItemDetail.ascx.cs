// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web;
using Rock.Web.UI;

namespace RockWeb.Plugins.church_ccv.Cms
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName("Simple Content Channel Item Detail")]
    [Category("CCV > CMS")]
    [Description("Displays only allowed attribute values for a content channel.")]
    [TextField( "Exclusion List", "The comma delimited list of Attribute Values Keys that should not be editable.", false )]
    public partial class SimpleContentChannelItemDetail : RockBlock, IDetailBlock
    {
        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
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
                ShowDetail(PageParameter("contentItemId").AsInteger(), PageParameter( "contentChannelId" ).AsInteger() );
            }
            else
            {
                var rockContext = new RockContext();
                ContentChannelItem item = GetContentItem();
                item.LoadAttributes();

                string excludeValues = GetAttributeValue( "ExclusionList" );
                List<string> exclusionList = excludeValues.Split( ',' ).ToList();

                phAttributes.Controls.Clear();
                Rock.Attribute.Helper.AddEditControls( item, phAttributes, false, BlockValidationGroup, exclusionList, false, 2 );
            }
        }

        /// <summary>
        /// Gets the bread crumbs.
        /// </summary>
        /// <param name="pageReference">The page reference.</param>
        /// <returns></returns>
        public override List<BreadCrumb> GetBreadCrumbs( PageReference pageReference )
        {
            var breadCrumbs = new List<BreadCrumb>();

            var itemIds = GetNavHierarchy().AsIntegerList();
            int? itemId = PageParameter( pageReference, "contentItemId" ).AsIntegerOrNull();
            if ( itemId != null )
            {
                itemIds.Add( itemId.Value );
            }

            foreach ( var contentItemId in itemIds )
            { 
                ContentChannelItem contentItem = new ContentChannelItemService( new RockContext() ).Get( contentItemId );
                if ( contentItem != null )
                {
                    breadCrumbs.Add( new BreadCrumb( contentItem.Title, pageReference ) );
                }
                else
                {
                    breadCrumbs.Add( new BreadCrumb( "New Content Item", pageReference ) );
                }
            }

            return breadCrumbs;
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the Click event of the lbSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void lbSave_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            ContentChannelItem contentItem = GetContentItem( rockContext );

            if ( contentItem != null &&
                ( IsUserAuthorized( Authorization.EDIT ) || contentItem.IsAuthorized( Authorization.EDIT, CurrentPerson ) ) )
            {
                contentItem.LoadAttributes( rockContext );
                Rock.Attribute.Helper.GetEditValues( phAttributes, contentItem );

                if ( !Page.IsValid || !contentItem.IsValid )
                {
                    // Controls will render the error messages                    
                    return;
                }

                rockContext.WrapTransaction( () =>
                {
                    rockContext.SaveChanges();
                    contentItem.SaveAttributeValues( rockContext );
                } );

                NavigateToParentPage();
            }
        }

        /// <summary>
        /// Handles the Click event of the lbCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void lbCancel_Click( object sender, EventArgs e )
        {
            NavigateToParentPage();
        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            ShowDetail( hfId.ValueAsInt(), null );
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Gets the type of the content.
        /// </summary>
        /// <param name="contentItemId">The content type identifier.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        private ContentChannelItem GetContentItem( RockContext rockContext = null )
        {
            rockContext = rockContext ?? new RockContext();
            var contentItemService = new ContentChannelItemService( rockContext );
            ContentChannelItem contentItem = null;

            int contentItemId = hfId.Value.AsInteger();
            if ( contentItemId != 0 )
            {
                contentItem = contentItemService
                    .Queryable( "ContentChannel,ContentChannelType" )
                    .FirstOrDefault( t => t.Id == contentItemId );
            }

            if ( contentItem == null )
            {
                var contentChannel = new ContentChannelService( rockContext ).Get( hfChannelId.Value.AsInteger() );
                if ( contentChannel != null )
                {
                    contentItem = new ContentChannelItem
                    {
                        Title = contentChannel.Name, // content channel items must have a title, so use the name of the channel
                        ContentChannel = contentChannel,
                        ContentChannelId = contentChannel.Id,
                        ContentChannelType = contentChannel.ContentChannelType,
                        ContentChannelTypeId = contentChannel.ContentChannelType.Id,
                        StartDateTime = RockDateTime.Now
                    };

                    if ( contentChannel.RequiresApproval )
                    {
                        contentItem.Status = ContentChannelItemStatus.PendingApproval;
                    }
                    else
                    {
                        contentItem.Status = ContentChannelItemStatus.Approved;
                        contentItem.ApprovedDateTime = RockDateTime.Now;
                        contentItem.ApprovedByPersonAliasId = CurrentPersonAliasId;
                    }

                    contentItemService.Add( contentItem );
                }
            }

            return contentItem;
        }

        public void ShowDetail( int contentItemId )
        {
            ShowDetail( contentItemId, null );
        }

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="contentItemId">The marketing campaign ad type identifier.</param>
        public void ShowDetail( int contentItemId, int? contentChannelId )
        {
            bool canEdit = IsUserAuthorized( Authorization.EDIT );
            hfId.Value = contentItemId.ToString();
            hfChannelId.Value = contentChannelId.HasValue ? contentChannelId.Value.ToString() : string.Empty;

            ContentChannelItem contentItem = GetContentItem();

            if ( contentItem == null )
            {
                // this block requires a valid ContentChannel in order to know which channel the ContentChannelItem belongs to, so if ContentChannel wasn't specified, don't show this block
                this.Visible = false;
                return;
            }

            if ( contentItem != null &&
                contentItem.ContentChannelType != null &&
                contentItem.ContentChannel != null &&
                ( canEdit || contentItem.IsAuthorized( Authorization.EDIT, CurrentPerson ) ) ) 
            {
                // get the list of items to NOT show
                string excludeValues = GetAttributeValue( "ExclusionList" );
                List<string> exclusionList = excludeValues.Split( ',' ).ToList();
                
                pnlEditDetails.Visible = true;

                hfId.Value = contentItem.Id.ToString();
                hfChannelId.Value = contentItem.ContentChannelId.ToString();

                string cssIcon = contentItem.ContentChannel.IconCssClass;
                if ( string.IsNullOrWhiteSpace( cssIcon ) )
                {
                    cssIcon = "fa fa-certificate";
                }

                lIcon.Text = string.Format( "<i class='{0}'></i>", cssIcon );
                lTitle.Text = contentItem.Title;
                
                contentItem.LoadAttributes();
                phAttributes.Controls.Clear();

                Rock.Attribute.Helper.AddEditControls( contentItem, phAttributes, true, BlockValidationGroup, exclusionList, false, 2 );
            }
            else
            {
                nbEditModeMessage.Text = EditModeMessage.NotAuthorizedToEdit( ContentChannelItem.FriendlyTypeName );
                pnlEditDetails.Visible = false;
            }
        }
        
        private List<string> GetNavHierarchy()
        {
            var qryParam = PageParameter( "Hierarchy" );
            if ( !string.IsNullOrWhiteSpace( qryParam ) )
            {
                return qryParam.SplitDelimitedValues( false ).ToList();
            }

            return new List<string>();
        }

        #endregion
    }
}