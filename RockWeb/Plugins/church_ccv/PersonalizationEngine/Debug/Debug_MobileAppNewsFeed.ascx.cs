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
using System.ComponentModel;
using Rock.Model;
using Rock.Web.UI;
using Rock.Data;
using church.ccv.PersonalizationEngine.Data;
using church.ccv.PersonalizationEngine.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rock.Web.UI.Controls;
using Rock.Attribute;
using Rock;

namespace RockWeb.Plugins.church_ccv.PersonalizationEngine
{
    [DisplayName( "Debug - Mobile App News Feed" )]
    [Category( "CCV > Personalization Engine" )]
    [Description( "Displays a debug preview of a Campaign as seen on the Mobile App News Feed." )]
    [CodeEditorField( "Lava Template", "The lava template to use to for rendering.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, true, "", "", 6 )]
    public partial class Debug_MobileAppNewsFeed : RockBlock
    {
        #region Properties
        
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

            Campaign campaignForCard = null;

            // first see if there's a parameter forcing an override of the campaign to display
            int? debugCampaignId = PageParameter( "DebugCampaignId" ).AsIntegerOrNull( );
            if ( debugCampaignId.HasValue )
            {
                campaignForCard = PersonalizationEngineUtil.GetCampaign( debugCampaignId.Value );
            }
           
            try
            {
                JObject jsonBlob = JObject.Parse( campaignForCard.ContentJson );

                JObject campaignBlob = jsonBlob["MobileAppNewsFeed"].ToObject<JObject>( );

                // try getting values for each piece of the campaign
                string campaignTitle = campaignBlob[ "title" ].ToString( );
                string campaignBody = campaignBlob[ "body" ].ToString( );
                string campaignImage = campaignBlob[ "img" ].ToString( );
                string campaignLinkUrl = campaignBlob[ "link" ].ToString( );
                
                // get the "skip details page" value
                bool campaignSkipDetailsPage = false;
                bool.TryParse( campaignBlob[ "skip-details-page" ].ToString( ), out campaignSkipDetailsPage );
                            
                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                mergeFields.Add( "CampaignTitle", campaignTitle );
                mergeFields.Add( "CampaignBody", campaignBody );
                mergeFields.Add( "CampaignImage", campaignImage );
                mergeFields.Add( "CampaignLinkUrl", campaignLinkUrl );
                mergeFields.Add( "CampaignSkipDetailsPage", campaignSkipDetailsPage );

                string template = GetAttributeValue( "LavaTemplate" );
                lContent.Text = template.ResolveMergeFields( mergeFields );
            }
            catch
            {
            }
        }
        
        #endregion
    }
}
