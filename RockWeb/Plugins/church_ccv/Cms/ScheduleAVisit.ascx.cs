using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.church_ccv.Cms
{
    [DisplayName( "Schedule A Visit" )]
    [Category( "CCV > Cms" )]
    [Description( "Form used to preregister families for weekend service" )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Connection Status", "The connection status to use for new individuals (default: 'Web Prospect'.)", true, false, Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT, "", 0 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_RECORD_STATUS, "Record Status", "The record status to use for new individuals (default: 'Pending'.)", true, false, Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING, "", 1 )]
    [CampusesField( "Campuses", "Campuses that offer plan a visit scheduling", false, "", "", 3 )]

    public partial class ScheduleAVisit : RockBlock
    {
        Visit _visit;

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
                // bind campuses using block setting
                BindCampuses();
            }
            else
            {
                LoadVisit();

            }


        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            // bind campuses using block setting
            BindCampuses();
        }


        #region Adult Panel Events

        /// <summary>
        /// Handles SelectedIndexChanged event of the campus dropdown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ddlCampus_SelectedIndexChanged( object sender, EventArgs e )
        {
            // check for selected campus
            int campusId = ddlCampus.SelectedValueAsInt(true) ?? 0;

            if ( campusId != 0 )
            {
                // get selected campus
                CampusCache campus = CampusCache.Read( campusId );

                // bind future weekend dates to VisitDate dropdown
                BindVisitDateDropDown( campus );

                // show the visit date dropdown
                divVisitDate.Attributes["class"] = "";
            }
            else
            {
                // no campus selected, hide visit date dropdown
                divVisitDate.Attributes["class"] = "hidden";
                divServiceTime.Attributes["class"] = "hidden";
            }
        }

        /// <summary>
        /// Handles SelectedIndexChanged event of the visit date dropdown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ddlVisitDate_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( ddlVisitDate.SelectedValue.IsNotNullOrWhitespace() )
            {
                // check for selected campus
                int campusId = ddlCampus.SelectedValueAsInt( true ) ?? 0;

                if (campusId != 0)
                {
                    CampusCache campus = CampusCache.Read( campusId );

                    BindServiceTimeDropDown( campus );

                    divServiceTime.Attributes["class"] = "";
                }
                else
                {
                    // no campus selected, hide visit date dropdowns
                    divVisitDate.Attributes["class"] = "hidden";
                    divServiceTime.Attributes["class"] = "hidden";
                }
            }
            else
            {
                // no visit date selected, hide service time dropdown
                divServiceTime.Attributes["class"] = "hidden";
            }


        }

        protected void btnAdultsNext_Click( object sender, EventArgs e )
        {
            // create a new Visit object if one doesnt exist
            if ( _visit == null )
            {
                _visit = new Visit();
            }

            // Check if person exists
            RockContext rockContext = new RockContext();

            PersonService personService = new PersonService( rockContext );
            Person person = new Person(); 
            person = personService.FindPerson( tbAdultFirstName.Text, tbAdultLastName.Text, tbAdultEmail.Text, false, false, false );

            // Populate visit object
            if (person != null)
            {
                _visit.PersonId = person.Id;
                _visit.FirstName = person.FirstName;
                _visit.LastName = person.LastName;
                _visit.Email = person.Email;

            } else
            {
                _visit.FirstName = tbAdultFirstName.Text;
                _visit.LastName = tbAdultLastName.Text;
                _visit.Email = tbAdultEmail.Text;
            }

            //_visit.VisitDate = dpVisitDate.SelectedDate;
            _visit.CampusId = ddlCampus.SelectedValue;
            _visit.ServiceTime = ddlServiceTime.SelectedValue;

            _visit.SpouseFirstName = tbSpouseFirstName.Text;
            _visit.SpouseLastName = tbSpouseLastName.Text;

            // set active tab to children
            _visit.ActiveTab = FormTab.Children;

            // write object to viewstate
            ViewState["Visit"] = _visit;

            // change to children panel
            ShowPanel( pnlChildren );
        }

        #endregion

        #region Children Panel Events

        protected void dppChildBDay_SelectedDatePartsChanged( object sender, EventArgs e )
        {

        }

        protected void btnChildrenBack_Click( object sender, EventArgs e )
        {

            // set active tab to adults
            _visit.ActiveTab = FormTab.Adults;

            // write object to viewstate
            ViewState["Visit"] = _visit;


            // change to adults panel
            ShowPanel( pnlAdults );
        }

        protected void btnChildrenNext_Click( object sender, EventArgs e )
        {


            // change to submit
            ShowPanel( pnlSubmit );
        }

        protected void btnChildrenAddAnother_Click( object sender, EventArgs e )
        {

        }

        #endregion

        #region Submit panel Events

        protected void btnSubmitBack_Click( object sender, EventArgs e )
        {

            // change to children panel
            ShowPanel( pnlChildren );
        }

        protected void btnSubmitNext_Click( object sender, EventArgs e )
        {



            // change to success panel
            ShowPanel( pnlSuccess );
        }

        #endregion

        #region Progress Navigation Events

        protected void btnProgressAdults_Click( object sender, EventArgs e )
        {

        }

        protected void btnProgressChildren_Click( object sender, EventArgs e )
        {

        }

        #endregion

        #region Methods

        #region Bind Methods

        /// <summary>
        /// Bind campuses
        /// </summary>
        private void BindCampuses()
        {
            // setup campus picker
            ddlCampus.Items.Clear();
            ddlCampus.Items.Add( new ListItem( "", "" ) );

            // get campuses from block setting
            var campusGuidList = GetAttributeValue( "Campuses" ).Split( ',' ).AsGuidList();

            var campusList = CampusCache.All().Where( c => campusGuidList.Contains( c.Guid ) );

            // add each campus to the picker
            foreach ( var campus in campusList )
            {
                ddlCampus.Items.Add( new ListItem( campus.Name, campus.Id.ToString() ) );
            }
        }

        /// <summary>
        /// Bind upcoming dates for the specified campus
        /// </summary>
        /// <param name="campus"></param>
        private void BindVisitDateDropDown( CampusCache campus )
        {
            // setup drop down
            ddlVisitDate.Items.Clear();
            ddlVisitDate.Items.Add( new ListItem( "", "" ) );

            // check for sat or sun service times
            bool hasSaturday = campus.RawServiceTimes.Contains( "Saturday" );
            bool hasSunday = campus.RawServiceTimes.Contains( "Sunday" );

            // get next sat and sunday date
            DateTime today = DateTime.Today;

            int daysUntilSaturday = DaysUntil( DayOfWeek.Saturday, today );
            int daysUntilSunday = DaysUntil( DayOfWeek.Sunday, today );

            DateTime nextSaturday = today.AddDays( daysUntilSaturday );
            DateTime nextSunday = today.AddDays( daysUntilSunday );

            // loop 4 times to add 4 upcoming dates for sat and sunday
            for ( int i = 0; i < 4; i++ )
            {
                if ( hasSaturday )
                {
                    string satItemName = nextSaturday.ToString( "dddd, MMMM d" );
                    string satItemValue = nextSaturday.ToString( "dddd" );
                    ddlVisitDate.Items.Add( new ListItem( satItemName, satItemValue ) );

                    // increase 7 days to next saturday
                    nextSaturday = nextSaturday.AddDays( 7 );
                }

                if ( hasSunday )
                {
                    string sunItemName = nextSunday.ToString( "dddd, MMMM d" );
                    string sunItemValue = nextSunday.ToString( "dddd" );

                    ddlVisitDate.Items.Add( new ListItem( sunItemName, sunItemValue ) );

                    // increase 7 days to next sunday
                    nextSunday = nextSunday.AddDays( 7 );
                }
            }
        }

        /// <summary>
        /// Bind available service times for campus / day selected
        /// </summary>
        /// <param name="campus"></param>
        private void BindServiceTimeDropDown( CampusCache campus )
        {
            ddlServiceTime.Items.Clear();
            ddlServiceTime.Items.Add( new ListItem( "", "" ) );

            foreach ( var serviceTime in campus.ServiceTimes )
            {
                if ( serviceTime.Day == ddlVisitDate.SelectedValue)
                {
                    string time = serviceTime.Time.Replace( "%", "" ).Replace( "*", "" );
                    ddlServiceTime.Items.Add( new ListItem( time, time ) );
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Show a panel
        /// </summary>
        /// <param name="panel"></param>
        private void ShowPanel( Panel panel )
        {
            HideAllFormPanels();

            panel.Visible = true;
        }

        /// <summary>
        /// Hide all form panels
        /// </summary>
        private void HideAllFormPanels()
        {
            pnlAdults.Visible = false;
            pnlChildren.Visible = false;
            pnlSubmit.Visible = false;
        }

        /// <summary>
        /// Load Visit from View State
        /// </summary>
        /// <returns></returns>
        private bool LoadVisit()
        {
            _visit = ( Visit ) ViewState["Visit"];

            if ( _visit != null )
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Return the number of days until the specified day of the week
        /// </summary>
        /// <param name="dayOfWeek"></param>
        /// <param name="today"></param>
        /// <returns></returns>
        private int DaysUntil( DayOfWeek dayOfWeek, DateTime today )
        {
            return ( ( int ) dayOfWeek - ( int ) today.DayOfWeek + 7 ) % 7;
        }

        #endregion

        #endregion

        #region Helper Class
        [Serializable]
        protected class Visit
        {
            // Visit Info
            public DateTime? VisitDate { get; set; }
            public string CampusId { get; set; }
            public string ServiceTime { get; set; }
            
            // Adult
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string MobileNumber { get; set; }
            public int PersonId { get; set; }

            // Spouse
            public string SpouseFirstName { get; set; }
            public string SpouseLastName { get; set; }

            // Children
            public List<Child> Children { get; set; }

            // Form State
            public FormTab ActiveTab { get; set; }

            public Visit()
            {
            }
        }

        [Serializable]
        protected class Child
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public int PersonId { get; set; }
            public string Birthday { get; set; }
            public string Allergies { get; set; }
            public string Gender { get; set; }
            public string Grade { get; set; }

            public Child()
            {
            }                       
        }

        #endregion

        protected enum FormTab
        {
            Adults,
            Children,
            Submit
        }


    }
}