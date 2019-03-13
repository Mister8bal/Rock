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
using Rock.Web.UI.Controls;

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
                _visit = new Visit();

                WriteVisitToViewState();

                // bind campuses using block setting
                BindCampuses();
            }
            else
            {
                if ( LoadVisit() )
                {
                    RestoreFormState();
                }
            }

            // ensure we have a visit object
            if ( _visit == null )
            {
                // missing visit object, hide form and show error
                pnlForm.Visible = false;

                nbMessage.NotificationBoxType = Rock.Web.UI.Controls.NotificationBoxType.Danger;
                nbMessage.Text = "Error loading visit information from viewstate.";
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

        #region Global Navigation Events

        /// <summary>
        /// Handles the Back button control event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnFormBack_Click( object sender, EventArgs e )
        {
            // get the panels to navigate to from CommandName attribute on button
            Button button = sender as Button;

            string[] panels = button.CommandName.Split( ',' );

            Panel mainPanel = FindControl( panels[0] ) as Panel;
            Panel subPanel = new Panel();

            // if childrens panel need to determine whether to show form or question
            if ( panels[0] == "pnlChildren")
            {
                if (_visit.BringingChildren)
                {
                    // show form
                    subPanel = pnlChildrenForm;
                }
                else
                {
                    // show question
                    subPanel = pnlChildrenQuestion;
                }
            }
            else
            {
                // not the children panel, use panel specified
                subPanel = FindControl( panels[1] ) as Panel;
            }

            if ( mainPanel.IsNotNull() )
            {
                ShowFormPanel( mainPanel, subPanel );
            }
        }

        #endregion

        #region Adult Panel Events

        /// <summary>
        /// Handles SelectedIndexChanged event of the campus dropdown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ddlCampus_SelectedIndexChanged( object sender, EventArgs e )
        {
            // check for selection            
            if ( ddlCampus.SelectedValue.IsNotNullOrWhitespace() )
            {
                // selection found, set campusId in _visit and update viewstate
                _visit.CampusId = ddlCampus.SelectedValue;

                WriteVisitToViewState();

                // convert campusId to int
                int campusId = _visit.CampusId.AsInteger();

                // check for valid campus
                if ( campusId != 0 )
                {
                    // sync value to edit drop down
                    ddlEditCampus.SelectedValue = ddlCampus.SelectedValue;

                    // get selected campus
                    CampusCache campus = CampusCache.Read( campusId );

                    // bind future weekend dates to VisitDate dropdown
                    BindVisitDateDropDowns( campus );

                    // show the visit date dropdown
                    divVisitDate.Attributes["class"] = "";
                }
                else
                {
                    // invalid campus, hide visit date dropdown
                    divVisitDate.Attributes["class"] = "hidden";
                    divServiceTime.Attributes["class"] = "hidden";
                }
            }
            else
            {
                // no selection, hide visit date dropdown
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
            // check for selection value
            if ( ddlVisitDate.SelectedValue.IsNotNullOrWhitespace() )
            {
                if ( ProcessVisitDateDropdownSelection( ddlVisitDate ) )
                {
                    // set the values on the submit tab
                    lblSubmitVisitDate.Text = ddlVisitDate.SelectedItem.Text;
                    ddlEditVisitDate.SelectedValue = ddlVisitDate.SelectedValue;

                    // reset service time selections in case visit date changed
                    ddlServiceTime.ClearSelection();
                    ddlEditServiceTime.ClearSelection();

                    // unhide service time
                    divServiceTime.Attributes["class"] = "";
                }
                else
                {
                    // something failed, reset visit dropdowns
                    ResetVisitDropDowns();
                }
            }
            else
            {
                // no visit date selected
                // clear service time dropdown selections
                ddlServiceTime.ClearSelection();
                ddlEditServiceTime.ClearSelection();

                // hide service time dropdown on adult form
                divServiceTime.Attributes["class"] = "hidden";
            }
        }



        protected void btnAdultsNext_Click( object sender, EventArgs e )
        {
            // default to new person
            bool newPerson = true;

            // Check if person exists
            PersonService personService = new PersonService( new RockContext() );

            Person person = personService.FindPerson( tbAdultFirstName.Text, tbAdultLastName.Text, tbAdultEmail.Text, false, false, false );
            
            // Populate visit object
            if ( person != null)
            {
                // person exists
                newPerson = false;

                _visit.PersonId = person.Id;
                _visit.FirstName = person.FirstName;
                _visit.LastName = person.LastName;
                _visit.Email = person.Email;

            } else
            {
                // new person
                _visit.FirstName = tbAdultFirstName.Text;
                _visit.LastName = tbAdultLastName.Text;
                _visit.Email = tbAdultEmail.Text;

                // set active tab to children
                _visit.ActiveTab = FormTab.Children;
            }

            _visit.CampusId = ddlCampus.SelectedValue;
            _visit.VisitDate = ddlVisitDate.SelectedValue;
            _visit.ServiceTime = ddlServiceTime.SelectedValue;

            _visit.SpouseFirstName = tbSpouseFirstName.Text;
            _visit.SpouseLastName = tbSpouseLastName.Text;

            WriteVisitToViewState();

            // set submit details values
            lblSubmitCampus.Text = ddlCampus.SelectedItem.Text;
            ddlEditCampus.SelectedValue = ddlCampus.SelectedValue;

            lblSubmitVisitDate.Text = ddlVisitDate.SelectedItem.Text;
            ddlEditVisitDate.SelectedValue = ddlVisitDate.SelectedValue;

            lblSubmitServiceTime.Text = ddlServiceTime.SelectedItem.Text;
            ddlEditServiceTime.SelectedValue = ddlServiceTime.SelectedValue;


            if (newPerson)
            {
                if ( _visit.BringingChildren)
                {
                    // show children form
                    ShowFormPanel( pnlChildren, pnlChildrenForm );
                }
                else
                {
                    // show children question
                    ShowFormPanel( pnlChildren, pnlChildrenQuestion );
                }
            }
            else
            {
                // show existing person panel
                ShowFormPanel( pnlAdults, pnlAdultsExisting );
            }
        }

        protected void btnAdultsExistingNext_Click( object sender, EventArgs e )
        {

        }

        #endregion

        #region Children Panel Events

        protected void btnChildrenYes_Click( object sender, EventArgs e )
        {
            // update visit object and save it to viewstate
            _visit.BringingChildren = true;

            WriteVisitToViewState();

            // unhide nav buttons
            btnChildrenAddAnother.Visible = true;
            btnChildrenNext.Visible = true;

            ShowFormPanel( pnlChildren, pnlChildrenForm );
        }



        protected void btnChildrenNext_Click( object sender, EventArgs e )
        {


            // change to submit
            ShowFormPanel( pnlSubmit, null );
        }

        protected void btnChildrenAddAnother_Click( object sender, EventArgs e )
        {

        }

        protected void dppChildBDay_SelectedDatePartsChanged( object sender, EventArgs e )
        {

        }

        #endregion

        #region Submit panel Events

        protected void lbEditVisitDetails_Click( object sender, EventArgs e )
        {
            // toggle between contorls to show/hide labels vs drop downs
            lblSubmitCampus.Visible = !lblSubmitCampus.Visible;
            ddlEditCampus.Visible = !ddlEditCampus.Visible;

            lblSubmitVisitDate.Visible = !lblSubmitVisitDate.Visible;
            ddlEditVisitDate.Visible = !ddlEditVisitDate.Visible;

            lblSubmitServiceTime.Visible = !lblSubmitServiceTime.Visible;
            ddlEditServiceTime.Visible = !ddlEditServiceTime.Visible;

            // change the edit details text
            btnEditVisitDetails.Text = ( btnEditVisitDetails.Text == "Edit details" ? "Save Details" : "Edit details" );
        }

        protected void btnSubmitNext_Click( object sender, EventArgs e )
        {



            // change to success panel
            ShowFormPanel( pnlSuccess, null );
        }

        protected void ddlEditCampus_SelectedIndexChanged( object sender, EventArgs e )
        {
            // check for selection value
            if (ddlEditCampus.SelectedValue.IsNotNullOrWhitespace() )
            {

            }
            else
            {
                // no campus selected, clear visit date / service time dropdowns
                ResetVisitDropDowns();

                // hide visit date and service time on adults panel
                divVisitDate.Attributes["class"] = "hidden";
                divServiceTime.Attributes["class"] = "hidden";
            }
        }

        protected void ddlEditVisitDate_SelectedIndexChanged( object sender, EventArgs e )
        {
            // check for selection value
            if ( ddlEditVisitDate.SelectedValue.IsNotNullOrWhitespace() )
            {
                if ( ProcessVisitDateDropdownSelection( ddlEditVisitDate ) )
                {
                    // set the values on the adults tab
                    ddlVisitDate.SelectedValue = ddlEditVisitDate.SelectedValue;
                }
                else
                {
                    // something failed, clear dropdowns
                    ResetVisitDropDowns();
                }
            }
            else
            {
                // no visit date selected
                // clear service time dropdown selections
                ddlServiceTime.ClearSelection();
                ddlEditServiceTime.ClearSelection();

                // hide service time dropdown on adult form
                divServiceTime.Attributes["class"] = "hidden";
            }
        }

        protected void ddlEditServiceTime_SelectedIndexChanged( object sender, EventArgs e )
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
            // reset campus dropdowns before popluating
            ddlCampus.Items.Clear();
            ddlCampus.Items.Add( new ListItem( "", "" ) );
            // submit edit campus
            ddlEditCampus.Items.Clear();
            ddlEditCampus.Items.Add( new ListItem( "", "" ) );

            // get campuses from block setting
            var campusGuidList = GetAttributeValue( "Campuses" ).Split( ',' ).AsGuidList();

            var campusList = CampusCache.All().Where( c => campusGuidList.Contains( c.Guid ) );

            // add each campus to the dropdown lists
            foreach ( var campus in campusList )
            {
                ddlCampus.Items.Add( new ListItem( campus.Name, campus.Id.ToString() ) );
                ddlEditCampus.Items.Add( new ListItem( campus.Name, campus.Id.ToString() ) );
            }
        }

        /// <summary>
        /// Bind upcoming dates for the specified campus
        /// </summary>
        /// <param name="campus"></param>
        private void BindVisitDateDropDowns( CampusCache campus )
        {
            // reset visit date dropdowns before populating
            ddlVisitDate.Items.Clear();
            ddlVisitDate.Items.Add( new ListItem( "", "" ) );
            ddlEditVisitDate.Items.Clear();
            ddlEditVisitDate.Items.Add( new ListItem( "", "" ) );

            // check for sat or sun service times
            bool hasSaturday = campus.RawServiceTimes.Contains( "Saturday" );
            bool hasSunday = campus.RawServiceTimes.Contains( "Sunday" );

            // get next sat and sunday date
            DateTime today = DateTime.Today;

            int daysUntilSaturday = DaysUntil( DayOfWeek.Saturday, today );
            int daysUntilSunday = DaysUntil( DayOfWeek.Sunday, today );

            DateTime nextSaturday = today.AddDays( daysUntilSaturday );
            DateTime nextSunday = today.AddDays( daysUntilSunday );

            // loop 4 times to add 4 upcoming dates for sat and sunday to the dropdowns
            for ( int i = 0; i < 4; i++ )
            {
                if ( hasSaturday )
                {
                    ListItem satItem = new ListItem( nextSaturday.ToString( "dddd, MMMM d" ), nextSaturday.ToString( "dddd, MMMM d" ) );

                    ddlVisitDate.Items.Add( satItem );
                    ddlEditVisitDate.Items.Add( satItem );

                    // increase 7 days to next saturday
                    nextSaturday = nextSaturday.AddDays( 7 );
                }

                if ( hasSunday )
                {
                    ListItem sunItem = new ListItem( nextSunday.ToString( "dddd, MMMM d" ), nextSunday.ToString( "dddd, MMMM d" ) );

                    ddlVisitDate.Items.Add( sunItem );
                    ddlEditVisitDate.Items.Add( sunItem );

                    // increase 7 days to next sunday
                    nextSunday = nextSunday.AddDays( 7 );
                }
            }
        }

        /// <summary>
        /// Bind available service times for campus / day selected
        /// </summary>
        /// <param name="campus"></param>
        private void BindServiceTimeDropDowns( CampusCache campus )
        {
            // reset dropdowns before populating
            ddlServiceTime.Items.Clear();
            ddlServiceTime.Items.Add( new ListItem( "", "" ) );
            ddlEditServiceTime.Items.Clear();
            ddlEditServiceTime.Items.Add( new ListItem( "", "" ) );

            // add each service time for the campus
            foreach ( var serviceTime in campus.ServiceTimes )
            {
                // split the visit date value to get the day
                string[] ddlVisitdateArray = ddlVisitDate.SelectedValue.Split( ',' );

                // if service time day matches visit date selection add time
                if ( serviceTime.Day == ddlVisitdateArray[0])
                {
                    // remove special characters used for special needs, hearing impaired, etc
                    string time = serviceTime.Time.Replace( "%", "" ).Replace( "*", "" );

                    // add time to service time dropdowns
                    ListItem item = new ListItem( time, time );

                    ddlServiceTime.Items.Add( item );
                    ddlEditServiceTime.Items.Add( item );
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Show a panel
        /// </summary>
        /// <param name="panel"></param>
        private void ShowFormPanel( Panel mainPanel, Panel subPanel )
        {
            HideAllFormPanels();

            if ( subPanel.IsNotNull() )
            {
                subPanel.Visible = true;
            }

            mainPanel.Visible = true;

        }

        /// <summary>
        /// Hide all form panels
        /// </summary>
        private void HideAllFormPanels()
        {
            pnlAdults.Visible = false;
            pnlAdultsForm.Visible = false;
            pnlAdultsExisting.Visible = false;
            pnlChildren.Visible = false;
            pnlChildrenQuestion.Visible = false;
            pnlChildrenForm.Visible = false;
            pnlSubmit.Visible = false;
        }

        /// <summary>
        /// Write Visit to View State
        /// </summary>
        private void WriteVisitToViewState()
        {
            // write object to viewstate
            ViewState["Visit"] = _visit;
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
        /// Restore form fields to correct state
        /// </summary>
        private void RestoreFormState()
        {
            // ran into weird issues trying to restore form fields, for now just unhiding things. will circle back

            if ( ddlServiceTime.SelectedValue.IsNotNullOrWhitespace())
            {
                divSpouse.Attributes["class"] = "";
            }

            if ( _visit.BringingChildren )
            {
                btnChildrenAddAnother.Visible = true;
                btnChildrenNext.Visible = true;
            }
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

        /// <summary>
        /// Process Visit Date selected in a visit date dropdown
        /// </summary>
        /// <param name="dropDownList"></param>
        /// <returns></returns>
        private bool ProcessVisitDateDropdownSelection( RockDropDownList visitDateDropDownList )
        {
            // set VisitDate in _visit
            _visit.VisitDate = visitDateDropDownList.SelectedValue;

            WriteVisitToViewState();

            // update service time dropdown with times available for visit date selected
            int campusId = _visit.CampusId.AsInteger();

            if ( campusId != 0 )
            {
                CampusCache campus = CampusCache.Read( campusId );

                BindServiceTimeDropDowns( campus );

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Clear selections on all Visit Date related dropdowns
        /// </summary>
        private void ResetVisitDropDowns()
        {
            // clear submit details
            lblSubmitCampus.Text = "";
            lblSubmitVisitDate.Text = "";
            lblSubmitServiceTime.Text = "";

            // adults panel
            ddlCampus.ClearSelection();
            ddlVisitDate.Items.Clear();
            ddlServiceTime.Items.Clear();

            // submit panel
            ddlEditCampus.ClearSelection();
            ddlEditVisitDate.Items.Clear();
            ddlEditServiceTime.Items.Clear();

            // hide visit date / service times dropdowns on adults page
            divVisitDate.Attributes["class"] = "hidden";
            divServiceTime.Attributes["class"] = "hidden";
        }

        #endregion

        #endregion

        #region Helper Class
        [Serializable]
        protected class Visit
        {
            // Visit Info
            public string VisitDate { get; set; }
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
            public bool BringingChildren { get; set; }
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