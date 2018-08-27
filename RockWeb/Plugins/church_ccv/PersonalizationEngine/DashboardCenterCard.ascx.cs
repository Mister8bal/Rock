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
    [DisplayName( "Dashboard Center Card" )]
    [Category( "CCV > Personalization Engine" )]
    [Description( "Displays personalized data on the dashboard's center card." )]
    [CodeEditorField( "Lava Template", "The lava template to use to format the group list.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, true, "{% include '~~/Assets/Lava/GroupListSidebar.lava' %}", "", 6 )]
    public partial class DashboardCenterCard : RockBlock
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
            
            // first try to get a relevant campaign for this person
            var campaignForCard = PersonalizationEngineUtil.GetRelevantCampaign( new Campaign.CampaignType[] { Campaign.CampaignType.WebsiteCard }, CurrentPerson.Id );
            if ( campaignForCard == null )
            {
                // if there's no relevant campaign, get all the "default" campaigns, and take the first one.
                var defaultCampaigns = PersonalizationEngineUtil.GetDefaultCampaigns( new Campaign.CampaignType[] { Campaign.CampaignType.WebsiteCard } );
                campaignForCard = defaultCampaigns[ 0 ];
            }

            JObject jsonBlob = JObject.Parse( campaignForCard.ContentJson );
                    
            string campaignImage =  jsonBlob["website-card-img"].ToString( );
            string campaignTitle = jsonBlob["title"].ToString( );
            string campaignSubTitle = jsonBlob["sub-title"].ToString( );
            string campaignBody = jsonBlob["body"].ToString( );
            string campaignLinkText = jsonBlob["link-text"].ToString( );
            string campaignLink = jsonBlob["link"].ToString( );
            
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
            mergeFields.Add( "CampaignImage", campaignImage );
            mergeFields.Add( "CampaignTitle", campaignTitle );
            mergeFields.Add( "CampaignSubTitle", campaignSubTitle );
            mergeFields.Add( "CampaignBody", campaignBody );
            mergeFields.Add( "CampaignLinkText", campaignLinkText );
            mergeFields.Add( "CampaignLink", campaignLink );

            string template = GetAttributeValue( "LavaTemplate" );
            lContent.Text = template.ResolveMergeFields( mergeFields );
        }
        
        #endregion
    }
}