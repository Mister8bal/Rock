
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
using church.ccv.PersonalizationEngine.Data;

namespace RockWeb.Plugins.church_ccv.PersonalizationEngine
{
    [DisplayName( "Persona List" )]
    [Category( "CCV > Personalization Engine" )]
    [Description( "Displays existing personas in a list." )]

    [LinkedPage("Detail Page")]
    public partial class PersonaList : RockBlock
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            PersonaFilter_Init( );
            PersonaGrid_Init( );
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
                PersonaFilter_Bind( );
                PersonaGrid_Bind( );
            }
        }

        #region Persona Filter
        protected void PersonaFilter_Init( )
        {
            rPersonaFilter.ApplyFilterClick += PersonaFilter_ApplyFilterClick;
        }   

        protected void PersonaFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            rPersonaFilter.SaveUserPreference( "Title", filterTbTitle.Text );

            PersonaGrid_Bind( );
        }

        protected void PersonaFilter_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            switch( e.Key )
            {
                case "Title":
                {
                    e.Value = filterTbTitle.Text;
                    break;
                }

                default:
                {
                    e.Value = string.Empty;
                    break;
                }
            }
        }
        
        protected void PersonaFilter_Bind( )
        {
            filterTbTitle.Text = rPersonaFilter.GetUserPreference( "Title" );
        }
        #endregion

        #region Persona Grid
        protected void PersonaGrid_Init( )
        {
            gPersonaGrid.DataKeyNames = new string[] { "Id" };

            // turn on only the 'add' button
            gPersonaGrid.Actions.Visible = true;
            gPersonaGrid.Actions.Enabled = true;
            gPersonaGrid.Actions.ShowBulkUpdate = false;
            gPersonaGrid.Actions.ShowCommunicate = false;
            gPersonaGrid.Actions.ShowExcelExport = false;
            gPersonaGrid.Actions.ShowMergePerson = false;
            gPersonaGrid.Actions.ShowMergeTemplate = false;
            
            gPersonaGrid.Actions.ShowAdd = IsUserAuthorized( Authorization.EDIT );
            gPersonaGrid.Actions.AddClick += PersonaGrid_AddClick;

            gPersonaGrid.GridRebind += PersonaGrid_GridRebind;
        }

        protected void PersonaGrid_AddClick( object sender, EventArgs e )
        {
            NavigateToDetailPage( null );
        }

        protected void PersonaGrid_RowSelected( object sender, RowEventArgs e )
        {
            NavigateToDetailPage( e.RowKeyId );
        }

        protected void PersonaGrid_Remove( object sender, RowEventArgs e )
        {
            PersonalizationEngineUtil.DeletePersona( e.RowKeyId );

            PersonaGrid_Bind( );
        }

        protected void PersonaGrid_GridRebind( object sender, GridRebindEventArgs e )
        {
            PersonaGrid_Bind( );
        }

        protected void PersonaGrid_Bind( )
        {
            // Grab all the personas
            using ( RockContext rockContext = new RockContext( ) )
            {
                var personaQuery = new Service<Persona>( rockContext ).Queryable( );

                // ---- Apply Filters ----

                // --Title
                if( string.IsNullOrWhiteSpace( filterTbTitle.Text ) == false )
                {
                    personaQuery = personaQuery.Where( c => c.Name.ToLower( ).Contains( filterTbTitle.Text.ToLower( ) ) );
                }
                
                // ---- Load Data ----
                var dataSource = personaQuery.Select( c => new
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description
                    });

                // --- Sorting ---
                if( gPersonaGrid.SortProperty != null )
                {
                    dataSource = dataSource.Sort( gPersonaGrid.SortProperty );
                }

                gPersonaGrid.DataSource = dataSource.ToList( );
                gPersonaGrid.DataBind( );
            }
        }
        #endregion

        #region Utility
        protected void NavigateToDetailPage( int? personaId )
        {
            var qryParams = new Dictionary<string, string>();
            int personaQueryId = personaId.HasValue ? personaId.Value : 0;
            qryParams.Add( "PersonaId", personaQueryId.ToString( ) );
            
            NavigateToLinkedPage( "DetailPage", qryParams );
        }
        #endregion
    }
}