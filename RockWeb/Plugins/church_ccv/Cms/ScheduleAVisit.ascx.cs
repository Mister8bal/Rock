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
        private Visit _visit;

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

                InitializeVisitObject();

                WriteVisitToViewState();

                // bind dropdowns
                BindCampuses();
                BindBDayYearDropDown();
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

                ClearErrorNotifications();

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
            RockDropDownList campusDropDownList = sender as RockDropDownList;

            // determine the tab we are on by the sender Id
            FormTab? currentTab = GetCurrentTab( campusDropDownList.ID );

            // campus changed, visit date and service time dropdowns are no longer valid
            ResetVisitDropDowns( false, true, true );

            if ( campusDropDownList.SelectedValue.IsNotNullOrWhitespace() || currentTab != null )
            {
                if ( campusDropDownList.SelectedValue.AsInteger() != 0 )
                {
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

            // Pretty sure this needs to be &&

            if ( visitDateDropDownList.SelectedValue.IsNotNullOrWhitespace() || currentTab != null || ddlCampus.SelectedValue.IsNotNullOrWhitespace() || day.IsNotNullOrWhitespace() )
            {
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
            RockDropDownList rockDropDownList = sender as RockDropDownList;

            // determine the tab we are on by the sender Id
            FormTab? currentTab = GetCurrentTab( rockDropDownList.ID );

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
                                 
        /// <summary>
        /// Handles btnAdultsNext click event 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnAdultsNext_Click( object sender, EventArgs e )
        {
            // default to new person
            bool newPerson = true;

            // Check if person exists
            PersonService personService = new PersonService( new RockContext() );

            var personQry = personService.GetByMatch( tbAdultFirstName.Text, tbAdultLastName.Text, tbAdultEmail.Text, false, false );

            if ( personQry.Count() > 0)
            {
                // reset radio button list
                rblExisting.Items.Clear();

                int selectedRadioIndex = -1;

                // add matching people to radio button list
                foreach ( var prsn in personQry )
                {
                    if (prsn.Age < 18)
                    {
                        // person found is under the age of 18, skip
                        continue;
                    }

                    // person(s) with matching info found
                    newPerson = false;

                    string maskedEmail = MaskEmail( prsn.Email );

                    string itemText = String.Format( "<div class=\"existing-person\"><p class=\"existing-person-name\">{0}</p><p>{1}</p></div>", prsn.FullName, maskedEmail );

                    rblExisting.Items.Add( new ListItem( itemText, prsn.Guid.ToString() ) );

                    // check if they were previously selected
                    if ( _visit.PersonId == prsn.Id )
                    {
                        // since items added in order, the current person index should be 1 less than the count
                        selectedRadioIndex = rblExisting.Items.Count - 1;
                    }
                }
               
                // add none item
                rblExisting.Items.Add( new ListItem( "None of these people are me", "none" ) );

                // select person if already selected
                if ( selectedRadioIndex > -1 )
                {
                    rblExisting.SelectedIndex = selectedRadioIndex;
                }
            }
            
            if ( newPerson == true )
            {
                // new person - populate visit object
                _visit.FirstName = tbAdultFirstName.Text;
                _visit.LastName = tbAdultLastName.Text;
                _visit.Email = tbAdultEmail.Text;

                _visit.SpouseFirstName = tbSpouseFirstName.Text;
                _visit.SpouseLastName = tbSpouseLastName.Text;

                // set active tab to children
                _visit.ActiveTab = FormTab.Children;
            }

            // show next form panel
            if (newPerson)
            {
                _visit.ActiveTab = FormTab.Children;

                // enable adults progress button
                SetProgressButtonsState();

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

            WriteVisitToViewState();
        }
        
        /// <summary>
        /// Handles btnAdultsExistingNext click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnAdultsExistingNext_Click( object sender, EventArgs e )
        {
            bool hasError = true;

            // get the person info
            PersonService personService = new PersonService( new RockContext() );

            Person person = null;

            // person is selected
            if ( rblExisting.SelectedValue.IsNotNullOrWhitespace() && rblExisting.SelectedValue != "none" )
            {
                person = personService.Get( rblExisting.SelectedValue.AsGuid() );

                if ( person != null )
                {
                    // populate visit object
                    _visit.PersonId = person.Id;
                    _visit.FirstName = person.FirstName;
                    _visit.LastName = person.LastName;
                    _visit.Email = person.Email;
                    _visit.MobileNumber = person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ).ToString();

                    if ( _visit.MobileNumber.IsNotNullOrWhitespace() )
                    {
                        tbMobileNumber.Text = _visit.MobileNumber;
                    }

                    // populate spouse info
                    _visit.SpouseFirstName = person.GetSpouse().FirstName;
                    _visit.SpouseLastName = person.GetSpouse().LastName;

                    // dont add the kids if family ID is already in visit object - prevents kids added everytime back button is hit
                    if (_visit.FamilyId != person.GetFamily().Id)
                    {
                        _visit.FamilyId = person.GetFamily().Id;

                        // populate children if any
                        var familyMembers = person.GetFamilyMembers( false, null );

                        if ( familyMembers.Count() > 0 )
                        {
                            foreach ( var familyMember in familyMembers )
                            {
                                // check if they are a child
                                var groupTypeRole = familyMember.Person.GetFamilyRole();

                                if ( groupTypeRole != null && groupTypeRole.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid() )
                                {
                                    // skip if child is already in _visit object
                                    if ( _visit.Children.Any( a => a.PersonId == familyMember.PersonId ) )
                                    {
                                        continue;
                                    }

                                    // child found
                                    Person child = familyMember.Person;
                                    child.LoadAttributes();

                                    // populate new child object
                                    Child newChild = new Child();

                                    newChild.PersonId = child.Id;
                                    newChild.FirstName = child.FirstName;
                                    newChild.LastName = child.LastName;
                                    newChild.Gender = child.Gender;
                                    newChild.Birthday = child.BirthDate;
                                    newChild.Grade = child.GradeFormatted;
                                    newChild.Allergies = child.GetAttributeValue( "Allergy" );

                                    // add child to visit
                                    AddChildToVisit( newChild );

                                    // hide bringing children question
                                    _visit.BringingChildren = true;
                                }
                            }
                        }

                    }


                    hasError = false;
                }
            } 

            // person not selected or couldnt load person, proceed using new person
            if ( rblExisting.SelectedValue.IsNotNullOrWhitespace() && ( rblExisting.SelectedValue == "none" || person == null ) )
            {
                // reset _visit object incase we are here from back button
                InitializeVisitObject();

                ResetChildForm( true );

                // new person - populate visit object
                _visit.FirstName = tbAdultFirstName.Text;
                _visit.LastName = tbAdultLastName.Text;
                _visit.Email = tbAdultEmail.Text;

                _visit.SpouseFirstName = tbSpouseFirstName.Text;
                _visit.SpouseLastName = tbSpouseLastName.Text;

                // reset buttons on children form
                btnChildrenAddAnother.Visible = false;
                btnChildrenNext.Visible = false;

                hasError = false;
            }

            if ( !hasError )
            {
                // Navigate to children tab
                _visit.ActiveTab = FormTab.Children;

                // set children question panel as default
                Panel panelToShow = pnlChildrenQuestion;

                // enable adults progress button
                SetProgressButtonsState();

                if ( _visit.BringingChildren )
                {
                    if ( _visit.Children.Count > 0)
                    {
                        // show next button in children panel
                        btnChildrenNext.Visible = true;

                        // ensure the add another child in bottom nav is hidden
                        btnChildrenAddAnother.Visible = false;

                        // show existing children panel
                        panelToShow = pnlChildrenExisting;
                    }
                    else
                    {
                        // show children form
                        panelToShow = pnlChildrenForm;
                    }
                }
 
                // show children question
                ShowFormPanel( pnlChildren, panelToShow );

                nbAlertExisting.Text = "";
            }
            else
            {
                nbAlertExisting.NotificationBoxType = NotificationBoxType.Danger;
                nbAlertExisting.Text = "error";
            }

            WriteVisitToViewState();
        }

        private void ResetChildForm(bool resetMobileNumber )
        {
            tbChildFirstName.Text = "";
            tbChildLastName.Text = "";
            ResetBDayDropDowns( true, true, true );
            rblAllergies.ClearSelection();
            rblAllergies.Items[0].Attributes.Add( "class", "radio-inline" );
            rblAllergies.Items[1].Attributes.Add( "class", "radio-inline" );
            tbAllergies.Text = "";
            rblGender.ClearSelection();
            gpChildGrade.ClearSelection();

            if (resetMobileNumber)
            {
                tbMobileNumber.Text = "";
            }

        }

        private void InitializeVisitObject()
        {
            _visit = new Visit();
            _visit.Children = new List<Child>();

            ltlExistingChildrenHorizontal.Text = "";
            ltlExistingChildrenVertical.Text = "";
        }

        #endregion

        #region Children Panel Events

        protected void ShowChildrenForm_Click( object sender, EventArgs e )
        {
            Button button = sender as Button;

            // default to children form in case something goes wrong
            Panel panelToShow = pnlChildrenForm;

            if (button == btnNotMyChildren )
            {
                // if not children, ensure personID is cleared
                _visit.PersonId = -1;

                if (_visit.Children != null)
                {
                    // personclicked not my child, clear out children object
                    _visit.Children.Clear();

                    // clear out child lists
                    ltlExistingChildrenHorizontal.Text = "";
                    ltlExistingChildrenVertical.Text = "";
                }

                // change to children question panel
                panelToShow = pnlChildrenQuestion;
                btnChildrenAddAnother.Visible = false;
                btnChildrenNext.Visible = false;
            }
            else
            {
                // update visit object and save it to viewstate
                _visit.BringingChildren = true;

                // unhide nav buttons
                btnChildrenAddAnother.Visible = true;
                btnChildrenNext.Visible = true;
            }

            ShowFormPanel( pnlChildren, panelToShow );

            WriteVisitToViewState();
        }

        protected void btnChildrenAddAnother_Click( object sender, EventArgs e )
        {
            // ensure required fields are coplete before proceeding
            if ( !RequiredChildFormFieldsReady() )
            {
                nbAlertChildForm.NotificationBoxType = NotificationBoxType.Danger;
                nbAlertChildForm.Text = "Please complete required fields above";
            }
            else
            {
                // update mobile number in visit object
                _visit.MobileNumber = tbMobileNumber.Text;

                Child newChild = CreateChild();

                AddChildToVisit( newChild );

                // clear the form
                ResetChildForm( false );

                ClearErrorNotifications();

                WriteVisitToViewState();
            }
        }

        private void AddChildToVisit( Child newChild )
        {
            _visit.Children.Add( newChild );

            // add new child to existing children displayed
            string newChildName = String.Format( "<div class=\"existing-child\"><span>{0}</span ></div>", newChild.FirstName );
            ltlExistingChildrenHorizontal.Text += newChildName;
            ltlExistingChildrenVertical.Text += newChildName;
        }

        private Child CreateChild()
        {
            // build new child object
            Child newChild = new Child();

            newChild.FirstName = tbChildFirstName.Text;
            newChild.LastName = tbChildLastName.Text;

            if ( ddlChildBdayYear.SelectedValue.IsNotNullOrWhitespace() && ddlChildBdayMonth.SelectedValue.IsNotNullOrWhitespace() && ddlChildBdayDay.SelectedValue.IsNotNullOrWhitespace() )
            {
                newChild.Birthday = new DateTime( ddlChildBdayYear.SelectedValue.AsInteger(), ddlChildBdayMonth.SelectedValue.AsInteger(), ddlChildBdayDay.SelectedValue.AsInteger() );
            }

            if ( rblAllergies.SelectedValue == "Yes" )
            {
                newChild.Allergies = tbAllergies.Text;
            }

            if ( rblGender.SelectedValue.IsNotNullOrWhitespace() )
            {
                switch ( rblGender.SelectedValue )
                {
                    case "Boy":
                        newChild.Gender = Gender.Male;
                        break;
                    case "Girl":
                        newChild.Gender = Gender.Female;
                        break;
                    default:
                        newChild.Gender = Gender.Unknown;
                        break;
                }
            }

            newChild.Grade = gpChildGrade.SelectedValue;

            return newChild;
        }

        protected void btnChildrenNext_Click( object sender, EventArgs e )
        {
            Button button = sender as Button;

            if ( button == btnChildrenNo)
            {
                // update visit 
                _visit.BringingChildren = false;
            }

            WriteVisitToViewState();

            // change to submit
            ShowFormPanel( pnlSubmit, null );
        }

        protected void ddlChildBdayYear_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( ddlChildBdayYear.SelectedValue.IsNotNullOrWhitespace())
            {
                // year changed rebind month (also clears day) (in case leap year)
                BindBDayMonthDropDown();

                // remove error class
                ddlChildBdayYear.FormGroupCssClass = "";

                ddlChildBdayMonth.Visible = true;
                ddlChildBdayDay.Visible = false;
            }
            else
            {
                // add error class
                ddlChildBdayYear.FormGroupCssClass = "has-error";

                // no selection hide month / day dropdowns
                ddlChildBdayMonth.Visible = false;
                ddlChildBdayDay.Visible = false;
            }

            // always validate fields to add or remove errors after postback
            RequiredChildFormFieldsReady();
        }

        protected void ddlChildBdayMonth_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( ddlChildBdayMonth.SelectedValue.IsNotNullOrWhitespace())
            {
                // month changed rebind days (# of days likely different)
                BindBdayDayDropDown();

                // remove error class
                ddlChildBdayMonth.FormGroupCssClass = "";

                ddlChildBdayDay.Visible = true;
            }
            else
            {
                // add error class
                ddlChildBdayMonth.FormGroupCssClass = "";

                // no selection hide day dropdown
                ddlChildBdayDay.Visible = false;
            }

            // always validate fields to add or remove errors after postbacks
            RequiredChildFormFieldsReady();
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
            ddlCampus.Items.Add( new ListItem( "" ) );
            ddlEditCampus.Items.Clear();
            ddlEditCampus.Items.Add( new ListItem( "" ) );

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
            // reset visit date dropdown
            ResetVisitDropDowns( false, true, true );

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
            ResetVisitDropDowns( false, false, true );

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


        /// <summary>
        /// Bind years for child bday
        /// </summary>
        private void BindBDayYearDropDown()
        {
            // reset the year dropdown
            ddlChildBdayYear.Items.Clear();
            ddlChildBdayYear.Items.Add( new ListItem( "" ) );
            
            // add past 50 years to year dropdown..overkill? lol
            for ( int i = 0; i < 50; i++ )
            {
                ddlChildBdayYear.Items.Add( new ListItem( DateTime.Now.AddYears( -i ).Year.ToString() ) );
            }
        }

        /// <summary>
        /// Bind months for child bday
        /// </summary>
        private void BindBDayMonthDropDown()
        {
            // reset the month dropdown
            ResetBDayDropDowns( false, true, true );

            // create date object to work with, year/day doesnt matter, just need a month)
            DateTime date = new DateTime( 2019, 1, 1 );

            // add months to dropdown
            for ( int i = 0; i < 12; i++ )
            {
                ddlChildBdayMonth.Items.Add( new ListItem( date.ToString( "MMMM" ), date.Month.ToString() ) );
                date = date.AddMonths( 1 );
            }
        }

        /// <summary>
        /// Bind days for child bday
        /// </summary>
        private void BindBdayDayDropDown()
        {
            // reset the day dropdown
            ResetBDayDropDowns( false, false, true );

            // get days in month      
            int daysInMonth = DateTime.DaysInMonth( ddlChildBdayYear.SelectedValue.AsInteger(), ddlChildBdayMonth.SelectedValue.AsInteger() );

            for ( int i = 1; i <= daysInMonth; i++ )
            {
                ddlChildBdayDay.Items.Add( new ListItem( i.ToString() ) );
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
            pnlChildrenExisting.Visible = false;
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

            // set state of children form
            if ( _visit.BringingChildren )
            {
                btnChildrenAddAnother.Visible = true;
                btnChildrenNext.Visible = true;
            }
            else
            {
                btnChildrenAddAnother.Visible = false;
                btnChildrenNext.Visible = false;
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
                    btnProgressChildren.Attributes.Add( "class", "active" );

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
        /// Resets visit date dropdowns (adds a blank item after reset)
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
                ddlVisitDate.Items.Add( new ListItem( "", "" ) );
                ddlEditVisitDate.Items.Clear();
                ddlEditVisitDate.Items.Add( new ListItem( "", "" ) );

                // hide visit date dropdown on adults page
                divVisitDate.Attributes["class"] = "hidden";
            }

            // Service Time Dropdowns - force reset if campus or visit date was reset
            if ( serviceTime || visitDate || campus )
            {
                lblSubmitServiceTime.Text = "";
                ddlServiceTime.Items.Clear();
                ddlServiceTime.Items.Add( new ListItem( "", "" ) );
                ddlEditServiceTime.Items.Clear();
                ddlEditServiceTime.Items.Add( new ListItem( "", "" ) );

                // hide service time dropdown on adults page
                divServiceTime.Attributes["class"] = "hidden";
            }
        }

        /// <summary>
        /// Reset child bday dropdowns (adds a blank item after reset)
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        private void ResetBDayDropDowns(bool year, bool month, bool day)
        {
            // reset year
            if ( year )
            {
                ddlChildBdayYear.ClearSelection();
            }

            // reset month - force reset if year was reset
            if ( month || year)
            {
                ddlChildBdayMonth.Items.Clear();
                ddlChildBdayMonth.Items.Add( new ListItem( "", "" ) );

                // hide month dropdown
                ddlChildBdayMonth.Visible = false;
            }

            // reset day - force reset if year or month was reset
            if (day || month || year)
            {
                ddlChildBdayDay.Items.Clear();
                ddlChildBdayDay.Items.Add( new ListItem( "", "" ) );

                // hide day dropdown
                ddlChildBdayDay.Visible = false;
            }
        }

        /// <summary>
        /// Mask an email address
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        private string MaskEmail( string email )
        {
            string maskedEmail = "";

            string[] emailParts = email.Split( '@' );

            string[] domainParts = emailParts[1].Split( '.' );

            // add first letter of local-part
            maskedEmail = emailParts[0].Substring( 0, 1 );

            // add * for remaining characters in local-part
            for ( int i = 0; i < emailParts[0].Length - 1; i++ )
            {
                maskedEmail += "*";
            }

            maskedEmail += "@";

            // add first letter of domain
            maskedEmail += domainParts[0].Substring( 0, 1 );

            // add * for remaining characters in first part of domain
            for ( int i = 0; i < domainParts[0].Length - 1; i++ )
            {
                maskedEmail += "*";
            }

            // TODO Handle multiple sub domains aka dots

            maskedEmail += ".";

            // add * for remaining characters in second part of domain
            for ( int i = 0; i < domainParts[1].Length; i++ )
            {
                maskedEmail += "*";
            }

            return maskedEmail;
        }

        /// <summary>
        /// Check if all child form required inputs have values
        /// </summary>
        /// <returns></returns>
        private bool RequiredChildFormFieldsReady()
        {
            bool ready = true;

            if ( !ValidateFormField( tbMobileNumber ) )
            {
                ready = false;
            }

            if ( !ValidateFormField( tbChildFirstName ) )
            {
                ready = false;
            }

            if ( !ValidateFormField( tbChildLastName ) )
            {
                ready = false;
            }

            if ( !ValidateFormField( ddlChildBdayYear ) )
            {
                ready = false;
            }

            if ( !ValidateFormField( ddlChildBdayMonth ) )
            {
                ready = false;
            }

            if ( !ValidateFormField( ddlChildBdayDay ) )
            {
                ready = false;
            }

            return ready;
        }

        /// <summary>
        /// Validate a rock dropdown list for value
        /// </summary>
        /// <param name="rockDropDownList"></param>
        /// <returns></returns>
        private bool ValidateFormField( RockDropDownList rockDropDownList )
        {
            if ( rockDropDownList.SelectedValue.IsNullOrWhiteSpace() )
            {
                // no value
                rockDropDownList.FormGroupCssClass = "has-error";

                return false;
            }
            else
            {
                // has value
                rockDropDownList.FormGroupCssClass = "";

                return true;
            }
        }

        /// <summary>
        /// Validate a rock text box for value
        /// </summary>
        /// <param name="rockTextBox"></param>
        /// <returns></returns>
        private bool ValidateFormField( RockTextBox rockTextBox )
        {
            if ( rockTextBox.Text.IsNullOrWhiteSpace() )
            {
                // no value
                rockTextBox.FormGroupCssClass = "has-error";
                return false;
            }
            else
            {
                // has value
                rockTextBox.FormGroupCssClass = "";

                return true;
            }
        }

        /// <summary>
        /// Clear Error notifications
        /// </summary>
        private void ClearErrorNotifications()
        {
            nbMessage.Text = "";
            nbAlertExisting.Text = "";
            nbAlertChildForm.Text = "";
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
            public int FamilyId { get; set; }

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
            public DateTime? Birthday { get; set; }
            public string Allergies { get; set; }
            public Gender Gender { get; set; }
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