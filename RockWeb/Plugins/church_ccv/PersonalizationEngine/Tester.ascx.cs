
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
    [DisplayName( "Tester" )]
    [Category( "CCV > Personalization Engine" )]
    [Description( "Lists the campaigns and personas that fit a selected person." )]

    public partial class Tester : RockBlock
    {
        private readonly string _USER_PREF_PERSON = "PersonalizationEngineTester:Person";
        private readonly string _USER_PREF_TARGET_DATE = "PersonalizationEngineTester:TargetDate";

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            PersonasGrid_Init( );
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
                RestoreControls();
            }

            BindPerson();
        }

        protected void BindPerson( )
        {
            // populates the page with the campaigns & personas that fit the selected person
            

            // get the persona
            if ( ppTarget.PersonId.HasValue )
            {
                // // first get all the personas this person fits
                var personas = PersonalizationEngineUtil.GetPersonas( ppTarget.PersonId.Value );

                // set the personas grid
                if ( personas.Count > 0 )
                {
                    lNoPersona.Visible = false;
                }
                else
                {
                    lNoPersona.Visible = true;
                }
                gPersonas.Visible = true;
                gPersonas.DataSource = personas;
                gPersonas.DataBind();


                // Now get all Campaigns this person would see

                // start by getting all campaign types, since we wanna show everything
                var campaignTypes = new Service<CampaignType>( new RockContext() ).Queryable().AsNoTracking().Select( ct => ct.Name );
                string commaDelimitedTypes = string.Empty;
                foreach ( var campaignType in campaignTypes )
                {
                    commaDelimitedTypes += campaignType + ",";
                }
                commaDelimitedTypes = commaDelimitedTypes.TrimEnd( ',' );
                
                // now, get all relevant campaigns for this person across all campaign platforms
                var campaigns = PersonalizationEngineUtil.GetRelevantCampaign( commaDelimitedTypes, ppTarget.PersonId.Value, int.MaxValue, dtpTargetDate.SelectedDate );
                
                // set the campaigns grid
                gCampaigns.Visible = true;
                gCampaigns.DataSource = campaigns;
                gCampaigns.DataBind();

            }
            // if a person isn't selected, hide the persona / campaign panels
            else
            {
                gPersonas.Visible = false;
                gCampaigns.Visible = false;
            }
        }

        #region Personas Grid
        protected void PersonasGrid_Init()
        {
            gPersonas.Visible = true;
            gPersonas.DataKeyNames = new string[] { "Id" };
        }
        #endregion

        #region Campaigns Grid
        protected void CampaignsGrid_Init( )
        {
            gCampaigns.Visible = true;
            gCampaigns.DataKeyNames = new string[] { "Id" };
        }
        #endregion

        #region PersonPicker
        protected void RestoreControls()
        {
            // Person Picker
            ppTarget.PersonId = GetUserPreference( _USER_PREF_PERSON ).AsIntegerOrNull();
            if ( ppTarget.PersonId != null )
            {
                var person = new PersonService( new RockContext() ).Get( ppTarget.PersonId ?? -1 );
                ppTarget.SetValue( person );
            }

            // Target DateTime
            DateTime? value = GetUserPreference( _USER_PREF_TARGET_DATE ).AsDateTime();
            if ( value.HasValue )
            {
                dtpTargetDate.SelectedDate = value;
            }
            else
            {
                dtpTargetDate.SelectedDate = DateTime.Now;
            }

        }

        protected void ppPerson_SelectPerson( object sender, EventArgs e )
        {
            SetUserPreference( _USER_PREF_PERSON, ppTarget.PersonId.ToStringSafe() );
            BindPerson();
        }
        #endregion

        protected void dpTargetDate_TextChanged( object sender, EventArgs e )
        {
            SetUserPreference( _USER_PREF_TARGET_DATE, dtpTargetDate.SelectedDate.ToString() );
            BindPerson();
        }
    }
}