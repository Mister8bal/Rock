using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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

        #region Global Events

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

            switch ( panels[0] )
            {
                case "pnlAdults":
                    _visit.ActiveTab = FormTab.Adults;
                    subPanel = FindControl( panels[1] ) as Panel;
                    break;
                case "pnlChildren":
                    _visit.ActiveTab = FormTab.Children;
                    if ( _visit.BringingChildren )
                    {
                        // show form
                        subPanel = pnlChildrenForm;
                    }
                    else
                    {
                        // show question
                        subPanel = pnlChildrenQuestion;
                    }
                    break;
                default:
                    subPanel = FindControl( panels[1] ) as Panel;
                    break;
            }

            WriteVisitToViewState();

            if ( mainPanel.IsNotNull() )
            {
                SetProgressButtonsState();

                ShowFormPanel( mainPanel, subPanel );
            }
        }

        /// <summary>
        /// Handles a campus drop down selection change event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void CampusDropDown_SelectedIndexChanged( object sender, EventArgs e )
        {
            // convert sender to object
            RockDropDownList campusDropDownList = sender as RockDropDownList;

            // determine the tab we are on by the sender Id
            FormTab? currentTab = GetCurrentTab( campusDropDownList.ID );

            // campus changed, visit date and service time dropdowns are no longer valid
            ResetVisitDropDowns( false, true, true );

            //  ensure selection and current tab known         
            if ( campusDropDownList.SelectedValue.IsNotNullOrWhitespace() || currentTab != null )
            {
                // check for valid campus
                if ( campusDropDownList.SelectedValue.AsInteger() != 0 )
                {
                    // get selected campus
                    CampusCache campus = CampusCache.Read( campusDropDownList.SelectedValue.AsInteger() );

                    // set submit label 
                    lblSubmitCampus.Text = campusDropDownList.SelectedItem.Text;

                    // sync value to other campus drop down
                    switch ( currentTab )
                    {
                        case FormTab.Adults:
                            ddlEditCampus.SelectedValue = campusDropDownList.SelectedValue;
                            break;
                        case FormTab.Submit:
                            ddlCampus.SelectedValue = campusDropDownList.SelectedValue;
                            break;
                        default:
                            break;
                    }

                    // set campusId in _visit and update viewstate
                    _visit.CampusId = campusDropDownList.SelectedValue;
                    WriteVisitToViewState();

                    // bind future weekend dates to VisitDate dropdowns
                    BindVisitDateDropDowns( campus );

                    // show the visit date dropdown
                    divVisitDate.Attributes["class"] = "";
                }
                else
                {
                    // invalid campus, hide visit date dropdowns on adults tab
                    divVisitDate.Attributes["class"] = "hidden";
                    divServiceTime.Attributes["class"] = "hidden";
                }
            }
            else
            {
                // no selection or couldnt determine tab
                // clear campus dropdown selections
                ddlEditCampus.ClearSelection();
                ddlCampus.ClearSelection();

                // hide visit date dropdown
                divVisitDate.Attributes["class"] = "hidden";
                divServiceTime.Attributes["class"] = "hidden";

                // TODO: display error?

            }
        }

        /// <summary>
        /// Handles visit date dropdown selection change event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void VisitDateDropDown_SelectedIndexChanged( object sender, EventArgs e )
        {
            // convert sender to object
            RockDropDownList visitDateDropDownList = sender as RockDropDownList;

            // determine the tab we are on by the sender Id
            FormTab? currentTab = GetCurrentTab( visitDateDropDownList.ID );

            // visit date changed, service time dropdown is no longer valid
            ResetVisitDropDowns( false, false, true );

            // get the day selected, strip off date
            string day = "";
            int index = visitDateDropDownList.SelectedItem.Text.IndexOf( "," );

            if ( index > 0 )
            {
                day = visitDateDropDownList.SelectedItem.Text.Substring( 0, index );
            }

            // ensure selection, current tab known, campus selected, and day exists
            if ( visitDateDropDownList.SelectedValue.IsNotNullOrWhitespace() || currentTab != null || ddlCampus.SelectedValue.IsNotNullOrWhitespace() || day.IsNotNullOrWhitespace() )
            {
                // check for valid campus
                if ( ddlCampus.SelectedValue.AsInteger() != 0 )
                {
                    CampusCache campus = CampusCache.Read( ddlCampus.SelectedValue.AsInteger() );

                    // set submit label
                    lblSubmitVisitDate.Text = visitDateDropDownList.SelectedItem.Text;

                    // sync value to other campus drop down
                    switch ( currentTab )
                    {
                        case FormTab.Adults:
                            ddlEditVisitDate.SelectedValue = visitDateDropDownList.SelectedValue;
                            break;
                        case FormTab.Submit:
                            ddlVisitDate.SelectedValue = visitDateDropDownList.SelectedValue;
                            break;
                        default:
                            break;
                    }

                    // set VisitDate in _visit
                    _visit.VisitDate = visitDateDropDownList.SelectedValue;
                    WriteVisitToViewState();
                    
                    // bind service times to dropdowns                                               
                    BindServiceTimeDropDowns( campus, day );

                    // show service time dropdown
                    divServiceTime.Attributes["class"] = "";
                }
                else
                {
                    // invalid campus, hide visit date dropdowns on adults tab
                    divVisitDate.Attributes["class"] = "hidden";
                    divServiceTime.Attributes["class"] = "hidden";
                }
            }
            else
            {
                // no visit date selected or couldnt determine current tab
                // clear visit date selections
                ddlEditVisitDate.ClearSelection();
                ddlVisitDate.ClearSelection();

                // hide service time dropdown on adult form
                divServiceTime.Attributes["class"] = "hidden";

                // TODO: display error?

            }
        }

        /// <summary>
        /// Handles service time dropdown selection change event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ServiceTimeDropDown_SelectedIndexChanged( object sender, EventArgs e )
        {
            // convert sender to object
            RockDropDownList rockDropDownList = sender as RockDropDownList;

            // determine the tab we are on by the sender Id
            FormTab? currentTab = GetCurrentTab( rockDropDownList.ID );

            // check for selection value
            if ( rockDropDownList.SelectedValue.IsNotNullOrWhitespace() || currentTab != null )
            {                    
                // sync value to other service time drop down
                switch ( currentTab )
                {
                    case FormTab.Adults:
                        ddlEditServiceTime.SelectedValue = rockDropDownList.SelectedValue;

                        // unhide spouse form fields
                        divSpouse.Attributes["class"] = "";
                        break;
                    case FormTab.Submit:
                        ddlServiceTime.SelectedValue = rockDropDownList.SelectedValue;
                        break;
                    default:
                        break;
                }

                // set submit labal
                lblSubmitServiceTime.Text = rockDropDownList.SelectedItem.Text;

                // set service time in _visit
                _visit.ServiceTime = rockDropDownList.SelectedValue;
                WriteVisitToViewState();
            }
            else
            {
                // no service time selection or couldnt determine current tab
                // clear service time selections
                ddlEditServiceTime.ClearSelection();
                ddlServiceTime.ClearSelection();

                // TODO: display error?

            }
        }


        #endregion

        #region Adult Panel Events
                                 
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

            // enable adults progress button
            SetProgressButtonsState();

            // show next form panel
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
            // ensure dropdowns are reset
            ddlVisitDate.Items.Clear();
            ddlVisitDate.Items.Add( new ListItem( "", "" ) );
            ddlEditVisitDate.Items.Clear();
            ddlEditVisitDate.Items.Add( new ListItem( "", "" ) );

            // check for sat or sun service times
            bool hasSaturday = campus.RawServiceTimes.Contains( "Saturday" );
            bool hasSunday = campus.RawServiceTimes.Contains( "Sunday" );

            // TODO: Check for Easter / Christmas service times and handle

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
        private void BindServiceTimeDropDowns( CampusCache campus, string day )
        {
            // reset dropdowns before populating
            ddlServiceTime.Items.Clear();
            ddlServiceTime.Items.Add( new ListItem( "", "" ) );
            ddlEditServiceTime.Items.Clear();
            ddlEditServiceTime.Items.Add( new ListItem( "", "" ) );

            // add each service time for the campus
            foreach ( var serviceTime in campus.ServiceTimes )
            {
                // if service time day matches passed day
                if ( serviceTime.Day == day)
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
            // set state of progress buttons
            switch ( _visit.ActiveTab )
            {
                case FormTab.Adults:
                    btnProgressAdults.Enabled = false;
                    btnProgressChildren.Enabled = false;
                    break;
                case FormTab.Children:
                    btnProgressAdults.Enabled = true;
                    btnProgressChildren.Enabled = false;
                    break;
                case FormTab.Submit:
                    btnProgressAdults.Enabled = true;
                    btnProgressChildren.Enabled = true;
                    break;
                default:
                    break;
            }

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
        /// Restore progress buttons enabled state
        /// </summary>
        private void SetProgressButtonsState()
        {
            // set state of progress buttons
            switch ( _visit.ActiveTab )
            {
                case FormTab.Adults:
                    btnProgressAdults.Enabled = false;
                    btnProgressChildren.Enabled = false;
                    break;
                case FormTab.Children:
                    btnProgressAdults.Enabled = true;
                    btnProgressChildren.Enabled = false;
                    break;
                case FormTab.Submit:
                    btnProgressAdults.Enabled = true;
                    btnProgressChildren.Enabled = true;
                    break;
                default:
                    break;
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
        /// Process Campus selected in campus dropdown
        /// </summary>
        /// <param name="campusId"></param>
        /// <returns></returns>
        //private void ProcessCampusDropDownSelection( string campusName, int campusId, FormTab? currentTab )
        //{
        //    // check for valid campus
        //    if ( campusId != 0 )
        //    {
        //        // get selected campus
        //        CampusCache campus = CampusCache.Read( campusId );

        //        // set submit label 
        //        lblSubmitCampus.Text = campusName;

        //        // sync value to other campus drop down
        //        switch ( currentTab )
        //        {
        //            case FormTab.Adults:
        //                ddlEditCampus.SelectedValue = campusId.ToString();
        //                break;
        //            case FormTab.Submit:
        //                ddlCampus.SelectedValue = campusId.ToString();
        //                break;
        //            default:
        //                break;
        //        }

        //        // set campusId in _visit and update viewstate
        //        _visit.CampusId = campusId.ToString();
        //        WriteVisitToViewState();

        //        // bind future weekend dates to VisitDate dropdowns
        //        BindVisitDateDropDowns( campus );

        //        // show the visit date dropdown
        //        divVisitDate.Attributes["class"] = "";
        //    }
        //    else
        //    {
        //        // invalid campus, hide visit date dropdowns on adults tab
        //        divVisitDate.Attributes["class"] = "hidden";
        //        divServiceTime.Attributes["class"] = "hidden";
        //    }
        //}


        /// <summary>
        /// Clear selections on all Visit Date related dropdowns
        /// </summary>
        private void ResetVisitDropDowns(bool campus, bool visitDate, bool serviceTime )
        {
            // Campus dropdowns
            if ( campus )
            {
                lblSubmitCampus.Text = "";
                ddlCampus.ClearSelection();
                ddlEditCampus.ClearSelection();
            }

            // Visit Date Dropdowns - force reset if campus was reset
            if ( visitDate || campus )
            {
                lblSubmitVisitDate.Text = "";
                ddlVisitDate.Items.Clear();
                ddlEditVisitDate.Items.Clear();

                // hide visit date dropdown on adults page
                divVisitDate.Attributes["class"] = "hidden";
            }

            // Service Time Dropdowns - force reset if campus or visit date was reset
            if ( serviceTime || visitDate || campus )
            {
                lblSubmitServiceTime.Text = "";
                ddlServiceTime.Items.Clear();
                ddlEditServiceTime.Items.Clear();

                // hide service time dropdown on adults page
                divServiceTime.Attributes["class"] = "hidden";
            }
        }

        /// <summary>
        /// Returns current form tab using dropdownlist id
        /// </summary>
        /// <param name="dropDownListId"></param>
        /// <returns></returns>
        private FormTab? GetCurrentTab( string dropDownListId )
        {
            switch ( dropDownListId )
            {
                case "ddlCampus":
                    return FormTab.Adults;
                case "ddlVisitDate":
                    return FormTab.Adults;
                case "ddlServiceTime":
                    return FormTab.Adults;
                case "ddlEditCampus":
                    return FormTab.Submit;
                case "ddlEditVisitDate":
                    return FormTab.Submit;
                case "ddlEditServiceTime":
                    return FormTab.Submit;
                default:
                    return null;
            }
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