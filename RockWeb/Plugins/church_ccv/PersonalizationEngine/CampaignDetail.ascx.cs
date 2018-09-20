
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

namespace RockWeb.Plugins.church_ccv.PersonalizationEngine
{
    [DisplayName( "Campaign Detail" )]
    [Category( "CCV > Personalization Engine" )]
    [Description( "Displays the details of a campaign." )]

    [LinkedPage("Persona Detail Page")]
    public partial class CampaignDetail : RockBlock
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // first build the UI necessary for editing all the campaign types
            CreateCampaignTypeUI( );
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
                int? campaignId = PageParameter( "CampaignId" ).AsIntegerOrNull( );
                BindCampaign( campaignId );
            }
        }
        
        protected void BindCampaign( int? campaignId )
        {
            // populates the page with the details of the campaign

            // get the campaign
            if( campaignId.HasValue && campaignId.Value > 0 )
            {
                using ( RockContext rockContext = new RockContext( ) )
                {
                    Campaign campaign = new Service<Campaign>( rockContext ).Get( campaignId.Value );

                    tbCampaignName.Text = campaign.Name;
                    tbCampaignDesc.Text = campaign.Description;
                    dtpStartDate.SelectedDateTime = campaign.StartDate;
                    dtpEndDate.SelectedDateTime = campaign.EndDate;

                    // get the content as a parseable jObject. this has all the values for all the supported types
                    JObject contentJson = JObject.Parse( campaign.ContentJson );
                    
                    // now see which types this campaign supports, and populate all those fields
                    string[] types = campaign.Type.Split( ',' );
                    foreach( string type in types )
                    {
                        // contentJson is a dictionary where the Key is the Campaign Type, and the value is a dictionary with
                        // the key/values for the type
                        string typeContent = contentJson[ type ].ToString( );

                        // get the actual key/values for the fields supported by this campaign type
                        Dictionary<string,string> typeValues = JsonConvert.DeserializeObject<Dictionary<string, string>>( typeContent );

                        // find all the controls associated with this type and fill them in
                        foreach( WebControl control in phContentJson.Controls.OfType<WebControl>( ) )
                        {
                            // only take controls that begin with this campaign type
                            if( control.ID.StartsWith( type ) )
                            {
                                // if it's the check box, obviousyl check it
                                if ( control as CheckBox != null )
                                {
                                    ( control as CheckBox ).Checked = true;
                                }
                                else
                                {
                                    // otherwise, see which text box it is and populate it with the right value.
                                    RockTextBox textBox = control as RockTextBox;
                                    textBox.Enabled = true;

                                    string value = GetValueForTextBox( textBox.ID, typeValues );
                                    textBox.Text = value;
                                }
                            }
                        }
                    }
                }
            }

            // if there was no campaign id sent, we'll just create a new one on Save, and we can leave all values blank
        }

        #region Utility
        protected void NavigateToDetailPage( int? campaignId )
        {
        }

        protected string GetValueForTextBox( string textBoxId, Dictionary<string, string> typeValues )
        {
            // first split the textbox ID
            string[] textBoxParts = textBoxId.Split( '^' );
            
            foreach( KeyValuePair<string, string> kvPair in typeValues )
            {
                // go thru each value in the campaign type, and find the key that matches the text box
                // we know that [1] is the part of the textBox id with the type key
                if ( textBoxParts[ 1 ] == kvPair.Key )
                {
                    return kvPair.Value;
                }
            }

            return string.Empty;
        }
        #endregion

        #region CreateUI
        protected void CreateCampaignTypeUI( )
        {
            // A campaign can support multiple types - WebsiteCard, MobileAppNewsFeed, etc.
            // each TYPE has a json template that defines what fields it supports (Title, Description, Image, etc.)

            // This function takes each type, and renders all its fields as editable controls, along with a checkbox specifying whether
            // the campaign uses that type or not.
            using ( RockContext rockContext = new RockContext( ) )
            {
                // get each campaign type with its json template
                var campaignTypes = new Service<CampaignType>( rockContext ).Queryable( ).Select( ct => new { Name = ct.Name, ct.JsonTemplate } ).ToList( );

                phContentJson.Controls.Clear( );

                foreach ( var campaignType in campaignTypes )
                {
                    CreateCampaignTypeTemplateUI( campaignType.Name, campaignType.JsonTemplate );
                }
            }
        }
        
        protected void CreateCampaignTypeTemplateUI( string campaignTypeName, string jsonTemplate )
        {
            // Builds the UI representing an individual campaign type

            // first we'll render a wrapping div that organizes all of this type's fields.
            // Within that, we render the name of the type, a checkbox, and then its json template.
            // The template defines the fields that the campaign type supports.

            // For easy lookup, elements that need to be referenced are given an ID with a prefix of the campaign type name.
            
            // Render a "campaign-type-entry" wrapping div around the entire thing
            phContentJson.Controls.Add( new LiteralControl( "<div class=\"campaign-type\">" ) );
            
                // Render a "campaign-type-entry-header" that will wrap the campaign type name and its enabled check box
                phContentJson.Controls.Add( new LiteralControl( "<div class=\"campaign-type-header\">" ) );
                    LiteralControl typeHeader = new LiteralControl( "<h4>" + campaignTypeName + "</h4>" );
                    phContentJson.Controls.Add( typeHeader );

                    // create the checkbox controlling whether this campaign type should be used.
                    RockCheckBox cbCampaignTypeEnabled = new RockCheckBox( );
                    cbCampaignTypeEnabled.ID = campaignTypeName + "^" + "cbEnabled";
                    cbCampaignTypeEnabled.Label = "Enabled";
                    cbCampaignTypeEnabled.CheckedChanged += CbTemplateTypeEnabled_CheckedChanged;
                    cbCampaignTypeEnabled.ClientIDMode = ClientIDMode.Static;
                    cbCampaignTypeEnabled.AutoPostBack = true;
                    phContentJson.Controls.Add( cbCampaignTypeEnabled );
                phContentJson.Controls.Add( new LiteralControl( "</div>" ) ); // end "campaign-type-entry-header"
            
                // now render the fields as editable controls

                // deserialize the json template into a dictionary we can iterate over
                var templateItems = JsonConvert.DeserializeObject<Dictionary<string, string>>( jsonTemplate );
            
                foreach( KeyValuePair<string, string> templateItem in templateItems )
                {
                    // render a wrapper around the item
                    phContentJson.Controls.Add( new LiteralControl( "<div class=\"campaign-type-template-item\">" ) );
                
                        // render the actual editable field for this template item
                        RockTextBox valueBox = new RockTextBox( );
                        valueBox.ID = campaignTypeName + "^" + templateItem.Key + "^" + "tbValue";
                        valueBox.Label = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(templateItem.Key);
                        valueBox.Enabled = false;
                        valueBox.ClientIDMode = ClientIDMode.Static;
                        phContentJson.Controls.Add( valueBox );
                
                    phContentJson.Controls.Add( new LiteralControl( "</div>" ) ); // end "campaign-type-template-item"
                }
                
            phContentJson.Controls.Add( new LiteralControl( "</div>" ) ); //end "template-entry" div wrapper
        }

        protected void CbTemplateTypeEnabled_CheckedChanged( object sender, EventArgs e )
        {
            // this will enable/disable the fields associated with this check box's campaign type
            // it also controls whether the campaign uses this type or not on Save
            
            RockCheckBox cbCampaignTypeEnabled = sender as RockCheckBox;

            // first, get the campaign type associated with this checkbox. (always the first part of the ID per our design)
            string[] idParts = cbCampaignTypeEnabled.ID.Split( '^' );
            string campaignType = idParts[0];

            // now find all the editable fields for this type
            // since we built this ourselves, we don't need to worry about recursion
            foreach( WebControl control in phContentJson.Controls.OfType<WebControl>( ) )
            {
                // guard against the control being the actual checkbox
                if ( control != cbCampaignTypeEnabled )
                {
                    if ( control.ID.StartsWith( campaignType ) )
                    {
                        control.Enabled = cbCampaignTypeEnabled.Checked;
                    }
                }
            }
        }
        #endregion
    }
}