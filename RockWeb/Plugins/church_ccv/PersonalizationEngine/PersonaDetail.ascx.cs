
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

namespace RockWeb.Plugins.church_ccv.PersonalizationEngine
{
    [DisplayName( "Persona Detail" )]
    [Category( "CCV > Personalization Engine" )]
    [Description( "Displays the details of a persona." )]

    [LinkedPage("Campaign Detail Page")]
    public partial class PersonaDetail : RockBlock
    {
        protected List<string> SqlBlackList { get; set; }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // setup our sql blacklist to prevent destructive commands
            SqlBlackList = new List<string>( );
            SqlBlackList.Add( "ADD" );
            SqlBlackList.Add( "ALTER" );
            SqlBlackList.Add( "CREATE" );
            SqlBlackList.Add( "DELETE" );
            SqlBlackList.Add( "DROP" );
            SqlBlackList.Add( "TRUNCATE" );
            SqlBlackList.Add( "UPDATE" );

            CampaignsGrid_Init( );
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
                BindPersona( );
            }
        }
        
        protected void BindPersona( )
        {
            // populates the page with the details of the persona
            int? personaId = PageParameter( "PersonaId" ).AsIntegerOrNull( );
            
            // get the persona
            // if there was no persona id sent, we'll just create a new one on Save, and we can leave all values blank
            if( personaId > 0 )
            {
                Persona persona = PersonalizationEngineUtil.GetPersona( personaId.Value );
                
                tbPersonaName.Text = persona.Name;
                tbPersonaDesc.Text = persona.Description;
                tbPersonaRockSQL.Text = persona.RockSQL;
                
                //setup the campaigns area
                upnlCampaigns.Visible = true;
                
                // show the personas grid
                var personas = PersonalizationEngineUtil.GetCampaignsForPersona( personaId.Value );
                gCampaigns.DataSource = personas;
                gCampaigns.DataBind( );
            }
            // for new personas, hide the persona panel entirely so they can't edit them before putting the persona into the db
            else
            {
                upnlCampaigns.Visible = false;
            }
        }
        
        protected void btnSave_Click( object sender, EventArgs e )
        {
            // first, see if the sql entered is safe. if not, don't allow saving it.
            SQLError sqlError = ValidateSql( tbPersonaRockSQL.Text );
            if ( sqlError == SQLError.None )
            {
                using ( RockContext rockContext = new RockContext( ) )
                {
                    // first see if this is an existing or new persona
                    int? personaId = PageParameter( "PersonaId" ).AsIntegerOrNull( );

                    Service<Persona> personaService = new Service<Persona>( rockContext );
                    Persona persona = null;
                    if ( personaId > 0 )
                    {
                        persona = personaService.Get( personaId.Value );
                    }
                    else
                    {
                        persona = new Persona( );
                        personaService.Add( persona );
                    }

                    // set the static stuff
                    persona.Name = tbPersonaName.Text;
                    persona.Description = tbPersonaDesc.Text;
                    persona.RockSQL = tbPersonaRockSQL.Text;

                    rockContext.SaveChanges( );

                    RefreshCurrentPage( persona.Id );
                }
            }
            else
            {
                // display an error modal
                switch ( sqlError )
                {
                    case SQLError.IllegalSyntax:
                    {
                        maWarning.Show( "The Rock SQL for this persona has invalid content.", ModalAlertType.Information );
                        break;
                    }

                    case SQLError.NoPersonId:
                    {
                        maWarning.Show( "The Rock SQL for this persona isn't returning a single row by using @PersonId.", ModalAlertType.Information );
                        break;
                    }
                }
            }
        }

        #region Utility
        protected enum SQLError
        {
            None,
            IllegalSyntax,
            NoPersonId
        }

        protected SQLError ValidateSql( string sql )
        {
            // strip out any escape sequence characters
            sql = Regex.Replace( sql, "[\a\b\f\n\r\t\v]", " " );

            // make sure we're comparing all upper case
            string[] sqlWords = sql.ToUpper( ).Split( ' ' );

            foreach ( string illegalCommand in SqlBlackList )
            {
                // see if the sql words contain any of our blacklisted commands
                if ( sqlWords.Contains( illegalCommand ) )
                {
                    return SQLError.IllegalSyntax;
                }
            }

            // now check to make sure it's returning a single row, not a list of people
            if ( sqlWords.Contains( "@PERSONID" ) == false )
            {
                return SQLError.NoPersonId;
            }

            return SQLError.None;
        }

        void RefreshCurrentPage( int personaId )
        {
            // refresh the current page
            var queryParams = new Dictionary<string, string>( );
            queryParams.Add( "PersonaId", personaId.ToString( ) );
            NavigateToCurrentPage( queryParams );
        }
        #endregion
        
        #region Campaigns Grid
        protected void CampaignsGrid_Init( )
        {
            gCampaigns.Visible = true;
            gCampaigns.DataKeyNames = new string[] { "Id" };
            gCampaigns.Actions.ShowAdd = true;
            gCampaigns.Actions.AddClick += gCampaignsGrid_AddClick;
        }

        protected void gCampaignsGrid_AddClick( object sender, EventArgs e )
        {
            //setup the campaign modal dialog

            // first populate it with whatever campaigns are not already tied to this persona
            using ( RockContext rockContext = new RockContext( ) )
            {
                // note - we won't allow doing this if they're editing a new, unsaved persona
                int? personaId = PageParameter( "PersonaId" ).AsIntegerOrNull( );

                // take all the campaigns that are NOT tied to this persona. 
                // To do that, first get all the IDs for campaigns that ARE linked to this persona
                var linkedCampaigns = new Service<Linkage>( rockContext ).Queryable( ).Where( l => l.PersonaId == personaId.Value )
                                                                                      .Select( l => l.CampaignId );

                // and now take any campaign that is not in that list.
                var campaigns = new Service<Campaign>( rockContext ).Queryable( ).Where( c => linkedCampaigns.Contains( c.Id ) == false )
                                                                                 .Select( c => new { c.Id, c.Name }  )
                                                                                 .ToList( );

                if( campaigns.Count( ) > 0 )
                {
                    ddlCampaign.Items.Clear( );
                    ddlCampaign.AutoPostBack = false;

                    foreach( var campaign in campaigns )
                    {
                        ddlCampaign.Items.Add( new ListItem( campaign.Name, campaign.Id.ToString( ) ) );
                    }

                    mdAddCampaign.Show( );
                }
                else
                {
                    // if there are somehow no campaigns available, notify them via an alert rather than showing the add dialog
                    maWarning.Show("There are no campaigns available for this persona.", ModalAlertType.Information );
                }
            }
        }

        protected void CampaignsGrid_Delete( object sender, RowEventArgs e )
        {
            int? personaId = PageParameter( "PersonaId" ).AsIntegerOrNull( );

            int campaignId = e.RowKeyId;

            // remove the link between the campaign and persona
            PersonalizationEngineUtil.UnlinkCampaignFromPersona( campaignId, personaId.Value );

            RefreshCurrentPage( personaId.Value );
        }

        protected void CampaignsGrid_RowSelected( object sender, RowEventArgs e )
        {
            // take them to the campaign details page for the campaign they clicked on
            int campaignId = e.RowKeyId;

            NavigateToLinkedPage( "CampaignDetailPage", "CampaignId", campaignId );
        }
        #endregion

        #region Add Campaign Modal
        protected void mdAddCampaign_AddClick( object sender, EventArgs e )
        {
            // get the ID of the campaign selected
            int campaignId = int.Parse( ddlCampaign.SelectedValue );

            // get the persona id
            int? personaId = PageParameter( "PersonaId" ).AsIntegerOrNull( );

            // create a linkage in the table
            PersonalizationEngineUtil.LinkCampaignToPersona( campaignId, personaId.Value );
                
            mdAddCampaign.Hide( );

            // refresh the current page
            RefreshCurrentPage( personaId.Value );
        }
        #endregion
    }
}