
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

            PersonasGrid_Init( );
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
                BindCampaign( );
            }
        }
        
        protected void BindCampaign( )
        {
            // populates the page with the details of the campaign
            int? campaignId = PageParameter( "CampaignId" ).AsIntegerOrNull( );
            
            // get the campaign
            // if there was no campaign id sent, we'll just create a new one on Save, and we can leave all values blank
            if( campaignId > 0 )
            {
                Campaign campaign = PersonalizationEngineUtil.GetCampaign( campaignId.Value );

                tbCampaignName.Text = campaign.Name;
                tbCampaignDesc.Text = campaign.Description;
                tbPriorty.Text = campaign.Priority.ToString( );

                dtpStartDate.SelectedDate = campaign.StartDate;
                dtpEndDate.SelectedDate = campaign.EndDate;

                // it's possible that if this campaign is a work in progress, it might not have types setup yet.
                if ( string.IsNullOrWhiteSpace( campaign.ContentJson ) == false )
                {
                    // get the content as a parseable jObject. this has all the values for all the supported types
                    JObject contentJson = JObject.Parse( campaign.ContentJson );
                    
                    // now see which types this campaign supports, and populate all those fields
                    string[] types = campaign.Type.Split( ',' );
                    foreach ( string type in types )
                    {
                        // contentJson is a dictionary where the Key is the Campaign Type, and the value is a dictionary with
                        // the key/values for the type
                        string typeContent = contentJson[ type ].ToString( );

                        // get the actual key/values for the fields supported by this campaign type
                        Dictionary<string,string> typeValues = JsonConvert.DeserializeObject<Dictionary<string, string>>( typeContent );

                        // find all the controls associated with this type and fill them in
                        foreach ( WebControl control in phContentJson.Controls.OfType<WebControl>( ) )
                        {
                            // only take controls that begin with this campaign type
                            if ( control.ID.StartsWith( type ) )
                            {
                                // if it's the check box, obviously check it
                                if ( control as CheckBox != null )
                                {
                                    ( control as CheckBox ).Checked = true;
                                }
                                // make sure it's otherwise a text box, and not the preview button
                                else if ( control as TextBox != null )
                                {
                                    // otherwise, see which text box it is and populate it with the right value.
                                    RockTextBox textBox = control as RockTextBox;
                                    textBox.Enabled = true;

                                    string value = GetValueForTextBox( textBox.ID, typeValues );
                                    textBox.Text = value;
                                }
                                // if it IS the preview button, we know there's preview html for the type, so enable the button
                                else if ( control as Button != null )
                                {
                                    Button previewButton = control as Button;
                                    previewButton.Enabled = true;
                                }
                            }
                        }
                    }
                }

                //setup the personas area
                upnlPersonas.Visible = true;
                
                // show the personas grid
                var personas = PersonalizationEngineUtil.GetPersonasForCampaign( campaignId.Value );
                    
                gPersonas.Visible = true;
                gPersonas.DataSource = personas;
                gPersonas.DataBind( );
            }
            // for new campaigns, hide the persona panel entirely so they can't edit them before putting the campaign into the db
            else
            {
                upnlPersonas.Visible = false;
            }
        }

        protected void btnSave_Click( object sender, EventArgs e )
        {
            using ( RockContext rockContext = new RockContext( ) )
            {
                // first see if this is an existing or new campaign
                int? campaignId = PageParameter( "CampaignId" ).AsIntegerOrNull( );

                Service<Campaign> campaignService = new Service<Campaign>( rockContext );
                Campaign campaign = null;
                if ( campaignId > 0 )
                {
                    campaign = campaignService.Get( campaignId.Value );
                }
                else
                {
                    campaign = new Campaign( );
                    campaignService.Add( campaign );
                }
                
                // set the static stuff
                campaign.Name = tbCampaignName.Text;
                campaign.Description = tbCampaignDesc.Text;
                campaign.Priority = int.Parse( tbPriorty.Text );

                campaign.StartDate = dtpStartDate.SelectedDate.Value.Date;
                campaign.EndDate = dtpEndDate.SelectedDate.HasValue ? dtpEndDate.SelectedDate.Value.Date : new DateTime?( );

                // now get the types this campaign is using, along with the actual values for each type
                string campaignTypes = string.Empty;
                
                // this will store the full hierarchy with all templdate data for the campaign, and its only use is for converting to json.
                Dictionary<string, Dictionary<string, string>> templateData = new Dictionary<string, Dictionary<string, string>>( );
                

                // start by getting the checkbox controls, which will dictate the types this campaign uses
                var checkBoxes = phContentJson.Controls.OfType<CheckBox>( ).ToList( );

                // get all text boxes, as these store the campaign type values
                var textBoxes = phContentJson.Controls.OfType<TextBox>( ).ToList( );
                                
                foreach( CheckBox checkBox in checkBoxes )
                {
                    // obviously only take types for checkboxes that are checked
                    if( checkBox.Checked )
                    {
                        // split this checkbox's ID so we know the campaign type its for. (the campaign type is always the first piece)
                        string campaignType = checkBox.ID.Split( '^' )[0];

                        campaignTypes += campaignType + ",";

                        // now collect all the fields associated with this campaign type

                        // take all text boxes, which have an ID format of "campaign-type^label^tbControl" and take only those for this campaign type
                        var campaignTypeValues = textBoxes.Where( tb => tb.ID.Split('^')[0] == campaignType ).ToList( );

                        // now we'll go thru each control and copy its label and value into our dictionary
                        Dictionary<string, string> typeValues = new Dictionary<string, string>( );
                        foreach( TextBox campaignTypeValue in campaignTypeValues )
                        {
                            string[] idChunks = campaignTypeValue.ID.Split('^');
                            
                            // the second value is the label for the type value
                            typeValues.Add( idChunks[1], campaignTypeValue.Text );
                        }
                        templateData.Add( campaignType, typeValues );
                    }
                }
                
                // make sure there was at least one campaign type picked
                if ( string.IsNullOrWhiteSpace( campaignTypes ) == false )
                {
                    // strip off the ending campaign type comma
                    campaignTypes = campaignTypes.TrimEnd(',');

                    // now convert the data into a json blob
                    string jsonBlob = JsonConvert.SerializeObject( templateData );
                    campaign.ContentJson = jsonBlob;
                    campaign.Type = campaignTypes;
                }
                else
                {
                    // if they really want a blank campaign, they can do it
                    campaign.ContentJson = string.Empty;
                    campaign.Type = string.Empty;
                }

                rockContext.SaveChanges( );

                RefreshCurrentPage( campaign.Id );
            }
        }

        #region Utility
        void RefreshCurrentPage( int campaignId )
        {
            // refresh the current page
            var queryParams = new Dictionary<string, string>( );
            queryParams.Add( "CampaignId", campaignId.ToString( ) );
            NavigateToCurrentPage( queryParams );
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
                var campaignTypes = new Service<CampaignType>( rockContext ).Queryable( ).Select( ct => new { Name = ct.Name, Desc = ct.Description, ct.JsonTemplate, ct.DebugUrl } ).ToList( );

                phContentJson.Controls.Clear( );

                foreach ( var campaignType in campaignTypes )
                {
                    CreateCampaignTypeTemplateUI( campaignType.Name, campaignType.Desc, campaignType.JsonTemplate, campaignType.DebugUrl, rockContext );
                }
            }
        }
        
        protected void CreateCampaignTypeTemplateUI( string campaignTypeName, string campaignTypeDesc, string jsonTemplate, string previewHtml, RockContext rockContext )
        {
            // Builds the UI representing an individual campaign type

            // first we'll render a wrapping div that organizes all of this type's fields.
            // Within that, we render the name of the type, a checkbox, and then its json template.
            // The template defines the fields that the campaign type supports.

            // For easy lookup, elements that need to be referenced are given an ID with a prefix of the campaign type name.

            phContentJson.Controls.Add( new LiteralControl( "<div class =\"panel panel-block\">" ) );
                phContentJson.Controls.Add( new LiteralControl( "<div class=\"panel-heading\">" ) );
                    phContentJson.Controls.Add( new LiteralControl( "<div class=\"row col-sm-12\">" ) );
                        phContentJson.Controls.Add( new LiteralControl( "<h4 class=\"panel-title\">" + campaignTypeName + "</h4><br/>" ) );

                        phContentJson.Controls.Add( new LiteralControl( "<h5>" + campaignTypeDesc + "</h5>" ) );
            
                    // create the checkbox controlling whether this campaign type should be used.
                    RockCheckBox cbCampaignTypeEnabled = new RockCheckBox( );
                    cbCampaignTypeEnabled.ID = campaignTypeName + "^" + "cbEnabled";
                    cbCampaignTypeEnabled.Label = "Enabled";
                    cbCampaignTypeEnabled.CheckedChanged += CbTemplateTypeEnabled_CheckedChanged;
                    cbCampaignTypeEnabled.ClientIDMode = ClientIDMode.Static;
                    cbCampaignTypeEnabled.AutoPostBack = true;
                    phContentJson.Controls.Add( cbCampaignTypeEnabled );

                    phContentJson.Controls.Add( new LiteralControl( "</div>" ) ); // end "row col-sm-4"
                phContentJson.Controls.Add( new LiteralControl( "</div>" ) ); // end "panel-heading"

                    
                
            
                phContentJson.Controls.Add( new LiteralControl( "<div class=\"panel-body control-and-preview-wrapper\">" ) );

                    phContentJson.Controls.Add( new LiteralControl( "<div class=\"controls\">" ) );
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
                                valueBox.ValidateRequestMode = ValidateRequestMode.Disabled;
                                valueBox.ClientIDMode = ClientIDMode.Static;
                                valueBox.Rows = 3;
                                valueBox.TextMode = TextBoxMode.MultiLine;
                                phContentJson.Controls.Add( valueBox );
                
                            phContentJson.Controls.Add( new LiteralControl( "</div>" ) ); // end "campaign-type-template-item"
                        }
                    
                        // if previewing is enabled for this campaign type
                        if ( string.IsNullOrWhiteSpace( previewHtml ) == false )
                        {
                            // build a preview button tied to this campaign type
                            string updatePreviewFunc = "updatePreview_" + campaignTypeName + "();";

                            Button previewButton = new Button( );
                            previewButton.ClientIDMode = ClientIDMode.Static;
                            previewButton.ID = campaignTypeName + "^" + "btnPreview";
                            previewButton.Enabled = false;
                            previewButton.Text = "Preview";
                            previewButton.CssClass = "btn btn-default";
                            previewButton.OnClientClick = updatePreviewFunc + " return false;";
                            phContentJson.Controls.Add( previewButton );
                        }
                    phContentJson.Controls.Add( new LiteralControl( "</div>" ) ); // end "controls"
                    
                    // if there's preview html to use, render it now!
                    if ( string.IsNullOrWhiteSpace( previewHtml ) == false )
                    {
                        phContentJson.Controls.Add( new LiteralControl( "<div class=\"preview\">" ) );
                            LiteralControl previewHtmlControl = new LiteralControl( );
                            phContentJson.Controls.Add( previewHtmlControl );

                            var htmlContentService = new HtmlContentService( rockContext );
                            HtmlContent content = new HtmlContent( );
                            content.Content = previewHtml;
                            
                            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                            previewHtmlControl.Text = content.Content.ResolveMergeFields( mergeFields );

                        phContentJson.Controls.Add( new LiteralControl( "</div>" ) ); //end "preview" div wrapper        
                    }

                phContentJson.Controls.Add( new LiteralControl( "</div>" ) ); //end "panel-body" div wrapper        
                
            phContentJson.Controls.Add( new LiteralControl( "</div>" ) ); //end "panel panel-block" div wrapper
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

        #region Personas Grid
        protected void PersonasGrid_Init( )
        {
            gPersonas.Visible = true;
            gPersonas.DataKeyNames = new string[] { "Id" };
            gPersonas.Actions.ShowAdd = true;
            gPersonas.Actions.AddClick += gPersonasGrid_AddClick;
        }

        protected void gPersonasGrid_AddClick( object sender, EventArgs e )
        {
            //setup the persona modal dialog

            // first populate it with whatever personas are not already tied to this campaign
            using ( RockContext rockContext = new RockContext( ) )
            {
                // note - we won't allow doing this if they're editing a new, unsaved campaign
                int? campaignId = PageParameter( "CampaignId" ).AsIntegerOrNull( );

                // take all the personas that are NOT tied to this campaign. 
                // To do that, first get all the IDs for personas that ARE linked to this campaign
                var linkedPersonas = new Service<Linkage>( rockContext ).Queryable( ).Where( l => l.CampaignId == campaignId.Value )
                                                                                     .Select( l => l.PersonaId );

                // and now take any persona that is not in that list.
                var personas = new Service<Persona>( rockContext ).Queryable( ).Where( p => linkedPersonas.Contains( p.Id ) == false )
                                                                               .Select( p => new { p.Id, p.Name }  )
                                                                               .ToList( );

                if( personas.Count( ) > 0 )
                {
                    ddlPersona.Items.Clear( );
                    ddlPersona.AutoPostBack = false;

                    foreach( var persona in personas )
                    {
                        ddlPersona.Items.Add( new ListItem( persona.Name, persona.Id.ToString( ) ) );
                    }

                    mdAddPersona.Show( );
                }
                else
                {
                    // if there are somehow no personas available, notify them via an alert rather than showing the add dialog
                    maNoPersonasWarning.Show("There are no personas available for this campaign.", ModalAlertType.Information );
                }
            }
        }

        protected void PersonasGrid_Delete( object sender, RowEventArgs e )
        {
            int? campaignId = PageParameter( "CampaignId" ).AsIntegerOrNull( );

            int personaId = e.RowKeyId;

            // remove the link between the campaign and persona
            PersonalizationEngineUtil.UnlinkCampaignFromPersona( campaignId.Value, personaId );

            RefreshCurrentPage( campaignId.Value );
        }

        protected void PersonasGrid_RowSelected( object sender, RowEventArgs e )
        {
            // take them to the persona details page for the persona they clicked on
            int personaId = e.RowKeyId;

            NavigateToLinkedPage( "PersonaDetailPage", "PersonaId", personaId );
        }
        #endregion

        #region Add Persona Modal
        protected void mdAddPersona_AddClick( object sender, EventArgs e )
        {
            // get the ID of the persona selected
            int personaId = int.Parse( ddlPersona.SelectedValue );

            // get the campaign id
            int? campaignId = PageParameter( "CampaignId" ).AsIntegerOrNull( );

            // create a linkage in the table
            PersonalizationEngineUtil.LinkCampaignToPersona( campaignId.Value, personaId );
                
            mdAddPersona.Hide( );

            // refresh the current page
            RefreshCurrentPage( campaignId.Value );
        }
        #endregion
    }
}