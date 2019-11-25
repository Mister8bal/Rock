using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Data.Entity;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using church.ccv.CCVCore.PlanAVisit.Model;

namespace RockWeb.Plugins.church_ccv.PAV
{
    [DisplayName( "Schedule A Visit Form" )]
    [Category( "CCV > Cms" )]
    [Description( "Form used to preregister families for a weekend service" )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Connection Status", "The connection status to use for new individuals (default: 'Web Prospect'.)", true, false, Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT, "", 0 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_RECORD_STATUS, "Record Status", "The record status to use for new individuals (default: 'Pending'.)", true, false, Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING, "", 1 )]
    [CampusesField( "Campuses", "Campuses that offer visit scheduling", false, "", "", 3 )]
    [SchedulesField( "Service Schedules", "Service Schedules available for use", true, "", "", 4 )]
    [IntegerField("Number Of Weekends","Number of weekends to include in the selection of service times.",false,4,"",5)]
    [TextField("Exclude Dates","Comma Separated List of dates to exclude.  Dates should be entered in the format MM/DD/YYYY.",false,"","",6)]
    [SystemEmailField( "Confirmation Email Template", "System email template to use for the email confirmation.", true, "", "", 7 )]
    [WorkflowTypeField( "Schedule A Visit Workflow", "Workflow used by staff to process visit submitted from website", true, false, "", "", 8 )]

    public partial class ScheduleAVisitForm : RockBlock
    {
        const int AttributeId_Allergies = 676;
        const int AttributeId_HowDidYouHearAboutCCV = 719;
        const int DefinedTypeId_SourceofVisit = 33;
        const string DefaultCountry = "US";

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

                // set initial state
                _visit.MainPanel = MainPanel.Adults;
                _visit.SubPanelAdults = SubPanelAdults.Form;
                _visit.SubPanelChildren = SubPanelChildren.Question;

                // bind dropdowns
                BindCampuses();
                BindBDayYearDropDown();
                BindStatesDropDown( DefaultCountry );

                ViewState["Visit"] = _visit;
            }
            else
            {
                if ( LoadVisit() )
                {
                    UpdateFormState();
                }
            }

            // ensure we have a visit object
            if ( _visit == null )
            {
                // missing visit object, hide form and show error
                pnlForm.Visible = false;

                nbMessage.NotificationBoxType = NotificationBoxType.Danger;
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
            Button button = sender as Button;

            // if back button on submit page was pressed, ensure edit details is in correct state
            if ( button == btnSubmitBack )
            {
                SetSubmitEditDetailsToView();
            }

            // get the panel to navigate to from CommandName attribute on button
            Panel mainPanel = FindControl( button.CommandName ) as Panel;
            Panel subPanel = new Panel();

            switch ( mainPanel.ID )
            {
                case "pnlAdults":
                    _visit.MainPanel = MainPanel.Adults;

                    // if back button on existing person form or Adults progress navigation was hit we want to go to adult form panel
                    // else go to panel that is in _visit state
                    if ( button == btnAdultsExistingBack || button == btnProgressAdults )
                    {
                        subPanel = pnlAdultsForm;
                    }
                    else
                    {
                        subPanel = GetSubPanelControl( _visit.SubPanelAdults );
                    }

                    // reset children panel to existing or question - so they can cancel creating a child
                    if ( _visit.Children.Count > 0 )
                    {
                        _visit.SubPanelChildren = SubPanelChildren.Existing;
                    }
                    else
                    {
                        _visit.SubPanelChildren = SubPanelChildren.Question;

                        // ensure form buttons dont show
                        btnChildrenNext.Visible = false;
                        btnChildrenAddAnother.Visible = false;
                        btnNotMyChildren.Visible = false;
                    }

                    break;
                case "pnlChildren":
                    _visit.MainPanel = MainPanel.Children;

                    subPanel = GetSubPanelControl( _visit.SubPanelChildren );
                    break;
            }

            if ( mainPanel.IsNotNull() )
            {
                ClearErrorNotifications();

                ShowFormPanel( mainPanel, subPanel );
            }

            ViewState["Visit"] = _visit;
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
            MainPanel? currentMainPanel = GetCurrentMainPanel( campusDropDownList.ID );

            // campus changed, visit date and service time dropdowns are no longer valid
            ResetVisitDropDowns( false, true, true );

            if ( campusDropDownList.SelectedValue.IsNotNullOrWhitespace() || currentMainPanel != null )
            {
                if ( campusDropDownList.SelectedValue.AsInteger() != 0 )
                {
                    CampusCache campus = CampusCache.Read( campusDropDownList.SelectedValue.AsInteger() );
                    BindVisitDateDropDowns( campus );

                    lblSubmitCampus.Text = campusDropDownList.SelectedItem.Text;

                    // sync value to other campus drop down
                    switch ( currentMainPanel )
                    {
                        case MainPanel.Adults:
                            ddlEditCampus.SelectedValue = campusDropDownList.SelectedValue;
                            break;
                        case MainPanel.Submit:
                            ddlCampus.SelectedValue = campusDropDownList.SelectedValue;
                            break;
                        default:
                            break;
                    }

                    _visit.CampusId = campus.Id;
                    _visit.CampusName = campus.Name;
                    ViewState["Visit"] = _visit;

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
            MainPanel? currentMainPanel = GetCurrentMainPanel( visitDateDropDownList.ID );

            // visit date changed, service time dropdown is no longer valid
            ResetVisitDropDowns( false, false, true );

            // get the day selected, strip off date
            string day = "";
            int index = visitDateDropDownList.SelectedItem.Text.IndexOf( "," );

            if ( index > 0 )
            {
                day = visitDateDropDownList.SelectedItem.Text.Substring( 0, index );
            }

            if ( visitDateDropDownList.SelectedValue.IsNotNullOrWhitespace() && currentMainPanel != null && ddlCampus.SelectedValue.IsNotNullOrWhitespace() && day.IsNotNullOrWhitespace() )
            {
                if ( ddlCampus.SelectedValue.AsInteger() != 0 )
                {
                    CampusCache campus = CampusCache.Read( ddlCampus.SelectedValue.AsInteger() );

                    lblSubmitVisitDate.Text = visitDateDropDownList.SelectedItem.Text;

                    // sync value to other visit drop down
                    switch ( currentMainPanel )
                    {
                        case MainPanel.Adults:
                            ddlEditVisitDate.SelectedValue = visitDateDropDownList.SelectedValue;
                            break;
                        case MainPanel.Submit:
                            ddlVisitDate.SelectedValue = visitDateDropDownList.SelectedValue;
                            break;
                        default:
                            break;
                    }

                    _visit.VisitDate = DateTime.Parse( visitDateDropDownList.SelectedValue );

                    ViewState["Visit"] = _visit;

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
            MainPanel? currentMainPanel = GetCurrentMainPanel( rockDropDownList.ID );

            if ( rockDropDownList.SelectedValue.IsNotNullOrWhitespace() && currentMainPanel != null )
            {
                // sync value to other service time drop down
                switch ( currentMainPanel )
                {
                    case MainPanel.Adults:
                        ddlEditServiceTime.SelectedValue = rockDropDownList.SelectedValue;

                        // unhide adultTwo form fields
                        divAdultTwo.Attributes["class"] = "";
                        break;
                    case MainPanel.Submit:
                        ddlServiceTime.SelectedValue = rockDropDownList.SelectedValue;
                        break;
                    default:
                        break;
                }

                lblSubmitServiceTime.Text = rockDropDownList.SelectedItem.Text;

                _visit.ServiceTime = rockDropDownList.SelectedItem.Text;
                _visit.ServiceTimeScheduleId = rockDropDownList.SelectedItem.Value.AsInteger();

                btnAdultsNext.Enabled = true;

                ViewState["Visit"] = _visit;
            }
            else
            {
                // no service time selection or couldnt determine current tab
                ddlEditServiceTime.ClearSelection();
                ddlServiceTime.ClearSelection();

                btnAdultsNext.Enabled = false;
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

            PersonService personService = new PersonService( new RockContext() );

            // first look for matching people
            PersonService.PersonMatchQuery searchParameters = new PersonService.PersonMatchQuery(
                firstName: tbAdultOneFirstName.Text,
                lastName: tbAdultOneLastName.Text,
                email: tbAdultOneEmail.Text,
                mobilePhone: tbAdultFormMobile.Text );

            var personQry = personService.FindPersons( searchParameters, false, false );

            if ( personQry.Count() > 0 )
            {
                // matches found

                // reset radio button list
                rblExisting.Items.Clear();

                int selectedRadioIndex = -1;

                // add matching people to radio button list
                foreach ( var prsn in personQry )
                {
                    if ( prsn.Age < 18 )
                    {
                        // person found is under the age of 18, skip
                        continue;
                    }

                    // person(s) with matching info found
                    newPerson = false;

                    // scrub identifier 
                    string maskedIdentifier = "";

                    // scrub mobile phone if exists
                    PhoneNumber mobileNumber = prsn.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );

                    if ( mobileNumber.IsNotNull() )
                    {
                        maskedIdentifier = MaskPhoneNumber( mobileNumber.ToString() );
                    }
                    // no mobile phone, scrub email if exists
                    else if ( prsn.Email.IsNotNullOrWhitespace() )
                    {
                        maskedIdentifier = MaskEmail( prsn.Email );
                    }
                    // no contact info found
                    else
                    {
                        maskedIdentifier = "No contact info on file";
                    }

                    string itemText = String.Format( "<div class=\"existing-person\"><p class=\"existing-person-name\">{0}</p><p>{1}</p></div>", prsn.FullName, maskedIdentifier );

                    rblExisting.Items.Add( new ListItem( itemText, prsn.Guid.ToString() ) );

                    // check if they were previously selected
                    if ( _visit.AdultOnePersonId == prsn.Id )
                    {
                        // since items added in order, the current person index should be 1 less than the count
                        selectedRadioIndex = rblExisting.Items.Count - 1;
                    }
                }

                rblExisting.Items.Add( new ListItem( "None of these people are me", "none" ) );

                // select person if was previously selected
                if ( selectedRadioIndex > -1 )
                {
                    rblExisting.SelectedIndex = selectedRadioIndex;
                }
                // select none if was previosly selected
                else if ( _visit.NoneRadioSelected == true )
                {
                    rblExisting.SelectedIndex = rblExisting.Items.Count - 1;
                }
            }

            if ( newPerson == true )
            {
                _visit.AdultOneFirstName = tbAdultOneFirstName.Text;
                _visit.AdultOneLastName = tbAdultOneLastName.Text;
                _visit.AdultOneEmail = tbAdultOneEmail.Text;
                _visit.AdultTwoFirstName = tbAdultTwoFirstName.Text;
                _visit.AdultTwoLastName = tbAdultTwoLastName.Text;

                if ( tbAdultFormMobile.Text.IsNotNullOrWhitespace() )
                {
                    _visit.MobileNumber = PhoneNumber.CleanNumber( tbAdultFormMobile.Text );

                    // populate and hide mobile field on children form (so it passes validation)
                    tbChildrenFormMobile.Text = tbAdultFormMobile.Text;
                    tbChildrenFormMobile.Visible = false;
                }
            }

            // show next panel
            if ( newPerson )
            {
                // new person
                _visit.MainPanel = MainPanel.Children;

                // get current subpanel
                Panel subPanel = GetSubPanelControl( _visit.SubPanelChildren );

                ShowFormPanel( pnlChildren, subPanel );
            }
            else
            {
                // existing person
                _visit.SubPanelAdults = SubPanelAdults.Existing;

                ShowFormPanel( pnlAdults, pnlAdultsExisting );
            }

            ViewState["Visit"] = _visit;
        }

        /// <summary>
        /// Handles btnAdultsExistingNext click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnAdultsExistingNext_Click( object sender, EventArgs e )
        {
            PersonService personService = new PersonService( new RockContext() );

            Person person = null;

            if ( rblExisting.SelectedValue.IsNotNullOrWhitespace() && rblExisting.SelectedValue != "none" )
            {
                person = personService.Get( rblExisting.SelectedValue.AsGuid() );

                if ( person != null )
                {
                    _visit.AdultOnePersonId = person.Id;
                    _visit.AdultOneFirstName = person.FirstName;
                    _visit.AdultOneLastName = person.LastName;
                    _visit.AdultOneEmail = person.Email;
                    _visit.AdultTwoFirstName = tbAdultTwoFirstName.Text;
                    _visit.AdultTwoLastName = tbAdultTwoLastName.Text;

                    if ( tbAdultFormMobile.Text.IsNotNullOrWhitespace() )
                    {
                        _visit.MobileNumber = PhoneNumber.CleanNumber( tbAdultFormMobile.Text );
                    }
                    // mobile number not specified, check for phone number in profile
                    else
                    {
                        PhoneNumber personMobileNumber = person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );

                        if ( personMobileNumber.IsNotNull() )
                        {
                            _visit.MobileNumber = personMobileNumber.ToString();

                            tbAdultFormMobile.Text = personMobileNumber.ToString();
                        }
                    }

                    if ( _visit.MobileNumber.IsNotNullOrWhitespace() )
                    {
                        // we know the mobile number, populate (to pass validation) and hide from children's form
                        tbChildrenFormMobile.Text = _visit.MobileNumber;
                        tbChildrenFormMobile.Visible = false;
                    }

                    // dont add the kids if family ID is already in visit object - prevents kids from being added multiple times
                    if ( _visit.FamilyId != person.GetFamily().Id )
                    {
                        _visit.FamilyId = person.GetFamily().Id;

                        // family id changed - reload children if exists
                        _visit.Children.Clear();
                        ltlExistingChildrenHorizontal.Text = "";
                        ltlExistingChildrenVertical.Text = "";

                        var familyMembers = person.GetFamilyMembers( false, null );

                        if ( familyMembers.Count() > 0 )
                        {
                            foreach ( var familyMember in familyMembers )
                            {
                                var groupTypeRole = familyMember.Person.GetFamilyRole();

                                if ( groupTypeRole != null && groupTypeRole.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid() )
                                {
                                    // child found in family

                                    // skip if child is already in _visit object
                                    if ( _visit.Children.Any( a => a.PersonId == familyMember.PersonId ) )
                                    {
                                        continue;
                                    }

                                    Person child = familyMember.Person;
                                    child.LoadAttributes();

                                    // add child to visit
                                    Child newChild = new Child
                                    {
                                        PersonId = child.Id,
                                        FirstName = child.FirstName,
                                        LastName = child.LastName,
                                        Gender = child.Gender,
                                        Birthday = child.BirthDate,
                                        Grade = child.GradeFormatted,
                                        Allergies = child.GetAttributeValue( "Allergy" )
                                    };

                                    AddChildToVisit( newChild );

                                    _visit.SubPanelChildren = SubPanelChildren.Existing;

                                    // ensure form buttons are showing
                                    btnChildrenNext.Visible = true;
                                    btnChildrenAddAnother.Visible = true;
                                    btnNotMyChildren.Visible = true;
                                }
                            }
                        }
                        else
                        {
                            _visit.SubPanelChildren = SubPanelChildren.Question;

                            // ensure form buttons are hidden
                            btnChildrenNext.Visible = false;
                            btnChildrenAddAnother.Visible = false;
                            btnNotMyChildren.Visible = false;
                        }
                    }
                }
            }

            // person not selected or couldnt load person, proceed using new person
            if ( rblExisting.SelectedValue.IsNotNullOrWhitespace() && ( rblExisting.SelectedValue == "none" || person == null ) )
            {
                // reset visit object if none not already selected
                if ( _visit.NoneRadioSelected == false )
                {
                    ReInitializeAsNewPerson();
                }
            }

            _visit.MainPanel = MainPanel.Children;

            // get the subpanel to show
            Panel subPanel = GetSubPanelControl( _visit.SubPanelChildren );

            ShowFormPanel( pnlChildren, subPanel );

            nbAlertExisting.Text = "";

            ViewState["Visit"] = _visit;
        }

        #endregion

        #region Children Panel Events

        /// <summary>
        /// Handles ShowChildrenForm click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ShowChildrenForm_Click( object sender, EventArgs e )
        {
            Button button = sender as Button;

            // default to children form
            Panel subPanel = pnlChildrenForm;

            if ( button == btnNotMyChildren )
            {
                // resetup form as a new person
                ReInitializeAsNewPerson();

                subPanel = pnlChildrenQuestion;

                btnChildrenAddAnother.Visible = false;
                btnChildrenNext.Visible = false;
            }
            else
            {
                _visit.SubPanelChildren = SubPanelChildren.Form;

                tbChildrenFormMobile.Visible = tbChildrenFormMobile.Text.IsNullOrWhiteSpace();

                btnChildrenAddAnother.Visible = true;
                btnChildrenNext.Visible = true;
            }

            ShowFormPanel( pnlChildren, subPanel );

            ViewState["Visit"] = _visit;
        }

        /// <summary>
        /// Handles btnChildrenAddAnother click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnChildrenAddAnother_Click( object sender, EventArgs e )
        {
            if ( RequiredChildFormFieldsReady() )
            {
                // update mobile number in visit object if it doesnt exist
                if ( _visit.MobileNumber.IsNullOrWhiteSpace() && tbChildrenFormMobile.Text.IsNotNullOrWhitespace() )
                {
                    _visit.MobileNumber = PhoneNumber.CleanNumber( tbChildrenFormMobile.Text );
                    tbAdultFormMobile.Text = tbChildrenFormMobile.Text;

                    // populate / hide mobile field on children form for future children to pass validation
                    tbChildrenFormMobile.Visible = false;
                }

                Child newChild = CreateChild();

                AddChildToVisit( newChild );

                ResetChildForm();

                // reset window position to top of form
                ScriptManager.RegisterStartupScript( pnlChildrenForm, pnlChildrenForm.GetType(), "reset-scroll-" + pnlChildrenForm.ID, "var needScroll = true;", true );

                ClearErrorNotifications();

                ViewState["Visit"] = _visit;
            }
            else
            {
                // missing form fields, show error
                nbAlertChildForm.NotificationBoxType = NotificationBoxType.Danger;
                nbAlertChildForm.Text = "Please complete required fields above";
            }
        }

        /// <summary>
        /// Handles btnChildrenNext click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnChildrenNext_Click( object sender, EventArgs e )
        {
            bool hasError = true;

            Button button = sender as Button;

            if ( button == btnChildrenNo || _visit.SubPanelChildren == SubPanelChildren.Existing || AllChildFormFieldsEmpty() == true )
            {
                // ready to proceed
                hasError = false;
            }
            else if ( RequiredChildFormFieldsReady() )
            {
                // update mobile number in visit object if its different from text box
                if ( _visit.MobileNumber != tbChildrenFormMobile.Text )
                {
                    _visit.MobileNumber = PhoneNumber.CleanNumber( tbChildrenFormMobile.Text );
                    tbAdultFormMobile.Text = tbChildrenFormMobile.Text;
                }

                Child newChild = CreateChild();

                AddChildToVisit( newChild );

                ResetChildForm();

                // ready to proceed
                hasError = false;
            }
            else
            {
                // missing form fields, show error
                nbAlertChildForm.NotificationBoxType = NotificationBoxType.Danger;
                nbAlertChildForm.Text = "Please complete required fields above";
            }

            // if no error proceed to submit panel
            if ( !hasError )
            {
                _visit.MainPanel = MainPanel.Submit;

                ClearErrorNotifications();

                ShowFormPanel( pnlSubmit, null );
            }

            ViewState["Visit"] = _visit;
        }

        /// <summary>
        /// Handles ddlChildBdayYear selected index changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ddlChildBdayYear_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( ddlChildBdayYear.SelectedValue.IsNotNullOrWhitespace() )
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

        /// <summary>
        /// Handles ddlChildBdayMonth selected index changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ddlChildBdayMonth_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( ddlChildBdayMonth.SelectedValue.IsNotNullOrWhitespace() )
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

        /// <summary>
        /// Handles lbEditVisitDetails click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void lbEditVisitDetails_Click( object sender, EventArgs e )
        {
            // toggle between contorls to show/hide labels vs drop downs
            lblSubmitCampus.Visible = !lblSubmitCampus.Visible;
            ddlEditCampus.Visible = !ddlEditCampus.Visible;

            lblSubmitVisitDate.Visible = !lblSubmitVisitDate.Visible;
            ddlEditVisitDate.Visible = !ddlEditVisitDate.Visible;

            lblSubmitServiceTime.Visible = !lblSubmitServiceTime.Visible;
            ddlEditServiceTime.Visible = !ddlEditServiceTime.Visible;

            pnlEditCampus.Visible = !pnlEditCampus.Visible;
            pnlEditVisitDate.Visible = !pnlEditVisitDate.Visible;
            pnlEditServiceTime.Visible = !pnlEditServiceTime.Visible;

            btnEditVisitDetails.Text = ( btnEditVisitDetails.Text == "Edit details" ? "Save Details" : "Edit details" );

            // enable / disable submit button
            if ( ddlEditCampus.Visible )
            {
                btnSubmitNext.Enabled = false;
            }
            else
            {
                btnSubmitNext.Enabled = true;
            }
        }

        /// <summary>
        /// Handles btnSubmitNext click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnSubmitNext_Click( object sender, EventArgs e )
        {
            bool newFamily = true;

            RockContext rockContext = new RockContext();

            // get system values
            var familyGroupType = GroupTypeCache.Read( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid() );
            var adultRoleId = familyGroupType.Roles.FirstOrDefault( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ).Id;
            var childRoleId = familyGroupType.Roles.FirstOrDefault( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid() ).Id;
            var recordTypePersonId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
            var recordStatusValue = DefinedValueCache.Read( GetAttributeValue( "RecordStatus" ).AsGuid() ) ?? DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() );
            var connectionStatusValue = DefinedValueCache.Read( GetAttributeValue( "ConnectionStatus" ).AsGuid() ) ?? DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT.AsGuid() );
            var homeLocationType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() );
            var mobilePhoneType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );

            // servicis needed
            AttributeValueService attributeValueService = new AttributeValueService( rockContext );
            PersonService personService = new PersonService( rockContext );
            Service<PlanAVisit> planAVisitService = new Service<PlanAVisit>( rockContext );

            // person / family objects
            Group family = null;
            Person adultOne = null;
            Person adultTwo = null;
            string visitNoteText = "";
            string defaultCountry = "US";

            // use existing person if exists
            if ( _visit.AdultOnePersonId > 0 )
            {
                adultOne = personService.Get( _visit.AdultOnePersonId );

                if ( adultOne != null )
                {
                    family = adultOne.GetFamily();

                    newFamily = false;
                }
            }

            // create new family if no existing family
            // also failing back to this if a person was found, but could not load their family
            if ( newFamily )
            {
                adultOne = new Person
                {
                    FirstName = _visit.AdultOneFirstName,
                    LastName = _visit.AdultOneLastName,
                    Email = _visit.AdultOneEmail,
                    ConnectionStatusValueId = connectionStatusValue.Id,
                    RecordTypeValueId = recordTypePersonId,
                    RecordStatusValueId = recordStatusValue.Id
                };

                family = PersonService.SaveNewPerson( adultOne, rockContext, _visit.CampusId, false );

                _visit.FamilyId = family.Id;
                _visit.AdultOnePersonId = adultOne.Id;
            }

            if ( family == null )
            {
                // something failed
                nbAlertSubmit.NotificationBoxType = NotificationBoxType.Danger;
                nbAlertSubmit.Text = "Error getting/creating family";

                return;
            }

            // add person note
            visitNoteText = String.Format( "Visit scheduled for {0} on {1} at the {2} campus", _visit.ServiceTime, _visit.VisitDate, _visit.CampusName );

            SavePersonNote( adultOne.Id, visitNoteText );

            // update survey answer if they answered
            if ( rblSurvey.SelectedValue.IsNotNullOrWhitespace() )
            {
                DefinedValueService definedValueService = new DefinedValueService( rockContext );

                // get the DefinedValue that matches the survey answer
                DefinedValue surveyAnswer = definedValueService.Queryable().Where( a => a.DefinedTypeId == DefinedTypeId_SourceofVisit && a.Value == rblSurvey.SelectedValue ).SingleOrDefault();

                if ( surveyAnswer.IsNotNull() )
                {
                    // check to see if currently exists - to prevent duplicates if the attribute was created with person creation
                    AttributeValue attributeValueSurvey = attributeValueService.Queryable().Where( av => av.EntityId == adultOne.Id && av.AttributeId == AttributeId_HowDidYouHearAboutCCV ).SingleOrDefault();

                    if ( attributeValueSurvey == null )
                    {
                        // doesnt exist - create a new attribute value tied to the person, and of attribute type "HowDidYouHearAboutCCV"
                        attributeValueSurvey = new AttributeValue
                        {
                            EntityId = adultOne.Id,
                            AttributeId = AttributeId_HowDidYouHearAboutCCV
                        };
                        attributeValueService.Add( attributeValueSurvey );
                    }

                    attributeValueSurvey.Value = surveyAnswer.Guid.ToString();

                    _visit.SurveyResponse = rblSurvey.SelectedValue;
                }
            }

            // if mobile specified, update/add to person
            if ( _visit.MobileNumber.IsNotNullOrWhitespace() )
            {
                PhoneNumberService phoneNumberService = new PhoneNumberService( rockContext );

                PhoneNumber phoneNumber = phoneNumberService.Queryable()
                    .Where( a =>
                        a.PersonId == adultOne.Id &&
                        a.NumberTypeValueId.HasValue &&
                        a.NumberTypeValueId.Value == mobilePhoneType.Id )
                    .FirstOrDefault();

                if ( phoneNumber == null )
                {
                    // person doesnt have a mobile number, create new one
                    phoneNumber = new PhoneNumber
                    {
                        PersonId = adultOne.Id,
                        NumberTypeValueId = mobilePhoneType.Id
                    };

                    phoneNumberService.Add( phoneNumber );
                }

                phoneNumber.Number = _visit.MobileNumber;
                phoneNumber.IsMessagingEnabled = true;

                rockContext.SaveChanges();
            }

            // Handle address.  If address is entered, replace
            // existing address with the one entered and
            // set the old one as a previous address.
            if ( !string.IsNullOrWhiteSpace(tbStreet1.Text) )
            {
                if ( family != null )
                {
                    Guid? addressTypeGuid = Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid();
                    if ( addressTypeGuid.HasValue )
                    {
                        var groupLocationService = new GroupLocationService( rockContext );
                        var dvHomeAddressType = DefinedValueCache.Read( addressTypeGuid.Value );

                        var familyAddress = groupLocationService.Queryable()
                            .Where( l => l.GroupId == family.Id && l.GroupLocationTypeValueId == dvHomeAddressType.Id )
                            .FirstOrDefault();
                        var street1String = tbStreet1.ToString();

                        if ( !string.IsNullOrWhiteSpace( tbStreet1.Text ) )
                        {
                            // Create a new location from the address info.
                            var newLoc = new Location();
                            newLoc.Street1 = tbStreet1.Text;
                            newLoc.City = tbCity.Text;
                            newLoc.State = ddlState.Text;
                            newLoc.PostalCode = tbPostalCode.Text;
                            newLoc.Country = defaultCountry;

                            var newLocation = new LocationService( rockContext )
                                .Get( newLoc.Street1, newLoc.Street2, newLoc.City, newLoc.State, newLoc.PostalCode, newLoc.Country, family, true );

                            if ( familyAddress == null )
                            {
                                familyAddress = new GroupLocation();
                                groupLocationService.Add( familyAddress );
                                familyAddress.GroupLocationTypeValueId = dvHomeAddressType.Id;
                                familyAddress.GroupId = family.Id;
                                familyAddress.IsMailingLocation = true;
                                familyAddress.IsMappedLocation = true;
                            }
                            else if ( tbStreet1.Text != string.Empty )
                            {

                                //Check to see if the new address matches the existing address.
                                var existingLocation = familyAddress.Location;
                                if ( existingLocation.Guid != newLocation.Guid )
                                {

                                    // addresses are different, so save existing address 
                                    // as a previous address. 
                                    var previousAddress = new GroupLocation();
                                    groupLocationService.Add( previousAddress );

                                    var previousAddressValue = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_PREVIOUS.AsGuid() );
                                    if ( previousAddressValue != null )
                                    {
                                        previousAddress.GroupLocationTypeValueId = previousAddressValue.Id;
                                        previousAddress.GroupId = family.Id;
                                        previousAddress.Location = existingLocation;
                                    }
                                }
                            }

                            familyAddress.Location = newLocation;

                            // since there can only be one mapped location, set the other locations to not mapped
                            if ( familyAddress.IsMappedLocation )
                            {
                                var groupLocations = groupLocationService.Queryable()
                                    .Where( l => l.GroupId == family.Id && l.Id != familyAddress.Id ).ToList();

                                foreach ( var grouplocation in groupLocations )
                                {
                                    grouplocation.IsMappedLocation = false;
                                    grouplocation.IsMailingLocation = false;
                                }
                            }
                        }
                    }
                }
            }

            // if not a new family, check if adult two already in family
            if ( !newFamily && _visit.AdultTwoFirstName.IsNotNullOrWhitespace() )
            {
                GroupMember member = family.Members.FirstOrDefault( a => ( a.Person.FirstName == _visit.AdultTwoFirstName || a.Person.NickName == _visit.AdultTwoFirstName ) && a.GroupRoleId == adultRoleId );

                if ( member != null )
                {
                    adultTwo = personService.Get( member.Person.Guid );
                }
            }

            // if adult two not found, add new if specified
            if ( adultTwo == null && _visit.AdultTwoFirstName.IsNotNullOrWhitespace() )
            {
                adultTwo = new Person()
                {
                    FirstName = _visit.AdultTwoFirstName,
                    LastName = _visit.AdultTwoLastName.IsNotNullOrWhitespace() ? _visit.AdultTwoLastName : _visit.AdultOneLastName,
                    ConnectionStatusValueId = connectionStatusValue.Id,
                    RecordTypeValueId = recordTypePersonId,
                    RecordStatusValueId = recordStatusValue.Id
                };

                PersonService.AddPersonToFamily( adultTwo, true, family.Id, adultRoleId, rockContext );
            }

            if ( adultTwo != null )
            {
                // add person note
                SavePersonNote( adultTwo.Id, visitNoteText );
            }

            // add children to family if specified
            if ( _visit.Children.Count > 0 )
            {
                foreach ( var child in _visit.Children )
                {
                    // first check if already in family
                    GroupMember childFamilyMember = family.Members.FirstOrDefault( a => ( a.Person.FirstName == child.FirstName || a.Person.NickName == child.FirstName ) && a.Person.BirthDate == child.Birthday );

                    if ( childFamilyMember == null )
                    {
                        // no matching child found, add new family member
                        Person newChild = new Person()
                        {
                            FirstName = child.FirstName,
                            LastName = child.LastName,
                            ConnectionStatusValueId = connectionStatusValue.Id,
                            RecordTypeValueId = recordTypePersonId,
                            RecordStatusValueId = recordStatusValue.Id
                        };

                        PersonService.AddPersonToFamily( newChild, true, family.Id, childRoleId, rockContext );

                        // add person note
                        SavePersonNote( newChild.Id, visitNoteText );

                        // update optional attributes (only doing this for new chidren added to family)
                        if ( newChild != null && ( child.Allergies.IsNotNullOrWhitespace() || child.Gender != Gender.Unknown || child.Grade.IsNotNullOrWhitespace() ) )
                        {
                            newChild.SetBirthDate( child.Birthday );

                            if ( child.Allergies.IsNotNullOrWhitespace() )
                            {
                                // check to see if value currently exists - to prevent duplicates if the attribute was created with person creation
                                AttributeValue avAllergy = attributeValueService.Queryable().Where( av => av.EntityId == newChild.Id && av.AttributeId == AttributeId_Allergies ).SingleOrDefault();

                                if ( avAllergy == null )
                                {
                                    // doesnt exist - create a new attribute value tied to the person(child), and of attribute type "Allergy"
                                    avAllergy = new AttributeValue
                                    {
                                        EntityId = newChild.Id,
                                        AttributeId = AttributeId_Allergies
                                    };
                                    attributeValueService.Add( avAllergy );
                                }

                                // update with the new value
                                avAllergy.Value = child.Allergies;
                            }

                            if ( child.Gender != Gender.Unknown )
                            {
                                newChild.Gender = child.Gender;
                            }

                            if ( child.Grade.IsNotNullOrWhitespace() )
                            {
                                newChild.GradeOffset = child.Grade.AsInteger();
                            }
                        }
                    }
                }
            }

            // add new plan a visit
            PlanAVisit visit = new PlanAVisit
            {
                AdultOnePersonAliasId = adultOne.PrimaryAlias.Id,
                AdultTwoPersonAliasId = adultTwo.IsNotNull() ? adultTwo.PrimaryAlias.Id : ( int? ) null,
                ScheduledCampusId = _visit.CampusId,
                ScheduledDate = _visit.VisitDate,
                ScheduledServiceScheduleId = _visit.ServiceTimeScheduleId,
                BringingChildren = _visit.Children.Count > 0 ? true : false,
                SurveyResponse = _visit.SurveyResponse,
                CreatedDateTime = DateTime.Now,
                ModifiedDateTime = DateTime.Now,
                CreatedByPersonAliasId = adultOne.PrimaryAlias.Id,
                ModifiedByPersonAliasId = adultOne.PrimaryAlias.Id
            };

            planAVisitService.Add( visit );

            rockContext.SaveChanges();

            // add campus to confirmation message
            lblCampusVisit.Text = lblSubmitCampus.Text;

            // Email confirmation
            Guid confirmationEmailTemplateGuid = Guid.Empty;

            if ( !Guid.TryParse( GetAttributeValue( "ConfirmationEmailTemplate" ), out confirmationEmailTemplateGuid ) )
            {
                confirmationEmailTemplateGuid = Guid.Empty;
            }

            if ( confirmationEmailTemplateGuid != Guid.Empty )
            {
                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null );

                var visitMergeFields = new Dictionary<string, object>
                {
                    { "Person", adultOne },
                    { "Campus", _visit.CampusName },
                    { "Date", _visit.VisitDate },
                    { "Service", _visit.ServiceTime }
                };

                mergeFields.Add( "Visit", visitMergeFields );

                // Send confirmation email
                var publicApplicationRoot = GlobalAttributesCache.Read( rockContext ).GetValue( "PublicApplicationRoot" );

                var recipient = new List<RecipientData>
                {
                    new RecipientData( adultOne.Email, mergeFields )
                };

                var emailMessage = new RockEmailMessage( confirmationEmailTemplateGuid );
                emailMessage.AddRecipient( new RecipientData( adultOne.Email, mergeFields ) );
                emailMessage.AppRoot = ResolveRockUrl( "~/" );
                emailMessage.ThemeRoot = ResolveRockUrl( "~~/" );
                emailMessage.CreateCommunicationRecord = true;
                emailMessage.Send();
            }

            // Activate Plan A Visit workflow
            Guid? workflowTypeGuid = GetAttributeValue( "ScheduleAVisitWorkflow" ).AsGuidOrNull();

            if ( workflowTypeGuid.HasValue )
            {
                WorkflowTypeCache workflowType = WorkflowTypeCache.Read( workflowTypeGuid.Value );

                if ( workflowType != null )
                {
                    string workflowName = String.Format( "{0} {1}'s Scheduled Visit", _visit.AdultOneFirstName, _visit.AdultOneLastName );

                    Workflow workflow = Workflow.Activate( workflowType, workflowName );

                    if ( workflow.AttributeValues.ContainsKey( "Person" ) )
                    {
                        workflow.AttributeValues["Person"].Value = adultOne.PrimaryAlias.Guid.ToString();
                    }

                    if ( workflow.AttributeValues.ContainsKey( "VisitCampus" ) )
                    {
                        CampusCache campus = CampusCache.Read( _visit.CampusId, rockContext );

                        if ( campus != null )
                        {
                            workflow.AttributeValues["VisitCampus"].Value = campus.Guid.ToString();

                        }
                    }

                    if ( workflow.AttributeValues.ContainsKey( "ServiceSchedule" ) )
                    {
                        ScheduleService scheduleService = new ScheduleService( rockContext );

                        Schedule schedule = scheduleService.Get( _visit.ServiceTimeScheduleId );

                        if ( schedule.IsNotNull() )
                        {
                            workflow.AttributeValues["ServiceSchedule"].Value = schedule.Guid.ToString();
                        }
                    }

                    if ( workflow.AttributeValues.ContainsKey( "VisitId" ) )
                    {
                        workflow.AttributeValues["VisitId"].Value = visit.Id.ToString();
                    }

                    List<string> workflowErrors;
                    new WorkflowService( rockContext ).Process( workflow, adultOne, out workflowErrors );
                }
            }

            pnlForm.Visible = false;

            ShowFormPanel( pnlSuccess, null );

            ViewState["Visit"] = _visit;
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

            // get next sat and sunday date
            DateTime today = DateTime.Today;

            int daysUntilSaturday = DaysUntil( DayOfWeek.Saturday, today );
            int daysUntilSunday = DaysUntil( DayOfWeek.Sunday, today );

            string exclusions = GetAttributeValue( "ExcludeDates" );
            List<string> exclusionsStrings = new List<string>();
            List<DateTime> exclusionDates = new List<DateTime>();

            if ( exclusions.IsNotNullOrWhitespace() )
            {
                exclusionsStrings = exclusions.Split( ',' ).Select( sValue => sValue.Trim() ).ToList();

                // If nothing exists in the List at this point, we can
                // assume that there was a date entered, but only one.  
                // In this case, we simply add the single date string to the array.
                if(exclusionsStrings.Count() < 1 )
                {
                    exclusionsStrings.Add( exclusions );
                }
            }

            if ( exclusionsStrings.Count() > 0 )
            {
                foreach ( string dateStr in exclusionsStrings )
                {
                    DateTime exclusionDate;
                    if( DateTime.TryParse( dateStr, out exclusionDate ) )
                    {
                        exclusionDates.Add( exclusionDate );
                    }
                }
            }

            DateTime nextSaturday = today.AddDays( daysUntilSaturday );
            DateTime nextSunday = today.AddDays( daysUntilSunday );

            // Start a loop to check dates to see if a weekend date exists
            // that's not an excluded date.  We also add a checked count
            // to protect against infinite loops.  
            int dateCount = 0;
            int checkedCount = 0;
            int numberOfWeekends = int.Parse( GetAttributeValue( "NumberOfWeekends" ));
            do
            {
                bool hasWeekend = false;

                if ( hasSaturday && !exclusionDates.Contains( nextSaturday ) )
                {
                    ListItem satItem = new ListItem( nextSaturday.ToString( "dddd, MMMM d" ), nextSaturday.ToString( "dddd, MMMM d" ) );

                    ddlVisitDate.Items.Add( satItem );
                    ddlEditVisitDate.Items.Add( satItem );

                    hasWeekend = true;
                }

                if ( hasSunday && !exclusionDates.Contains( nextSunday ) )
                {
                    ListItem sunItem = new ListItem( nextSunday.ToString( "dddd, MMMM d" ), nextSunday.ToString( "dddd, MMMM d" ) );

                    ddlVisitDate.Items.Add( sunItem );
                    ddlEditVisitDate.Items.Add( sunItem );

                    hasWeekend = true;
                }

                // increase 7 days to next saturday
                nextSaturday = nextSaturday.AddDays( 7 );
                // increase 7 days to next sunday
                nextSunday = nextSunday.AddDays( 7 );

                // increment the date count if either a saturday or sunday
                // date exists. 
                if( hasWeekend )
                {
                    dateCount++;
                }

                checkedCount++;

            } while ( dateCount < numberOfWeekends && checkedCount <= 52);
        }

        /// <summary>
        /// Bind available service times for campus / day selected
        /// </summary>
        /// <param name="campus"></param>
        private void BindServiceTimeDropDowns( CampusCache campus, string day )
        {
            // reset dropdowns before populating
            ResetVisitDropDowns( false, false, true );

            // setup schedule service, schedule lookup list, and selected schedules
            RockContext rockContext = new RockContext();
            ScheduleService scheduleService = new ScheduleService( rockContext );

            var scheduleLookupList = scheduleService.Queryable().Where( a => a.Name != null && a.Name != "" ).ToList().Select( a => new
            {
                a.Id,
                a.Name
            } );

            var selectedScheduleIds = new ScheduleService( rockContext ).GetByGuids( this.GetAttributeValue( "ServiceSchedules" ).SplitDelimitedValues().AsGuidList() ).Select( a => a.Id ).ToList();

            foreach ( var serviceTime in campus.ServiceTimes )
            {
                // check if service time matches day selected
                if ( serviceTime.Day == day )
                {
                    string time = serviceTime.Time.Replace( "%", "" ).Replace( "*", "" ).Trim();

                    // look for a matching schedule from schedules block setting
                    string scheduleName = String.Format( "{0} {1}", day, time.RemoveSpaces() );

                    var scheduleLookup = scheduleLookupList.FirstOrDefault( a => a.Name == scheduleName );

                    if ( scheduleLookup != null && selectedScheduleIds.Contains( scheduleLookup.Id ) )
                    {
                        // schedule found, add to drop down lists
                        ListItem item = new ListItem( time, scheduleLookup.Id.ToString() );

                        ddlServiceTime.Items.Add( item );
                        ddlEditServiceTime.Items.Add( item );
                    }
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

            // add past 50 years (special needs kids can be any age) to year dropdown..overkill? lol
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

        private void BindStatesDropDown( string country )
        {
            string countryGuid = DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.LOCATION_COUNTRIES ) )
                .DefinedValues
                .Where( v => v.Value.Equals( country, StringComparison.OrdinalIgnoreCase ) )
                .Select( v => v.Guid )
                .FirstOrDefault()
                .ToString();

            var definedType = DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.LOCATION_ADDRESS_STATE ) );

            var stateDdlListValues = new List<Object>();
            stateDdlListValues.Add( new { Id = "", Value = "" } );

            var stateList = definedType
                .DefinedValues
                .Where( v =>
                    (
                        v.AttributeValues.ContainsKey( "Country" ) &&
                        v.AttributeValues["Country"] != null &&
                        v.AttributeValues["Country"].Value.Equals( countryGuid, StringComparison.OrdinalIgnoreCase )
                    ) ||
                    (
                        ( !v.AttributeValues.ContainsKey( "Country" ) || v.AttributeValues["Country"] == null ) &&
                        v.Attributes.ContainsKey( "Country" ) &&
                        v.Attributes["Country"].DefaultValue.Equals( countryGuid, StringComparison.OrdinalIgnoreCase )
                    ) )
                .OrderBy( v => v.Order )
                .ThenBy( v => v.Value )
                .Select( v => new { Id = v.Value, Value = v.Description } )
                .ToList();

            stateDdlListValues.AddRange( stateList );

            if ( stateDdlListValues.Any() )
            {

                string currentValue = ddlState.SelectedValue;

                ddlState.Items.Clear();
                ddlState.SelectedIndex = -1;
                ddlState.SelectedValue = null;
                ddlState.ClearSelection();
                
                ddlState.DataTextField = "Id";
                ddlState.DataSource = stateDdlListValues;
                ddlState.DataBind();

                if ( !string.IsNullOrWhiteSpace( currentValue ) )
                {
                    ddlState.SetValue( currentValue, "AZ" );
                }
            }
            
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Show a form panel
        /// </summary>
        /// <param name="mainPanel"></param>
        /// <param name="subPanel"></param>
        private void ShowFormPanel( Panel mainPanel, Panel subPanel )
        {
            HideAllFormPanels();

            if ( subPanel.IsNotNull() )
            {
                subPanel.Visible = true;
            }

            mainPanel.Visible = true;

            // set nav button states based off subpanel
            if ( subPanel == pnlChildrenForm || subPanel == pnlChildrenExisting )
            {
                btnChildrenNext.Visible = true;
            }

            if ( subPanel == pnlChildrenExisting )
            {
                btnChildrenAddAnother.Visible = false;
            }

            UpdateFormState();

            // reset window position to top of form
            ScriptManager.RegisterStartupScript( mainPanel, mainPanel.GetType(), "reset-scroll-" + mainPanel.ID, "var needScroll = true;", true );
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
        /// Initialize blank visit object
        /// </summary>
        private void InitializeVisitObject()
        {
            _visit = new Visit
            {
                Children = new List<Child>(),
                AdultOnePersonId = -1   // -1 indicates a new person needs to be created
            };
        }

        /// <summary>
        /// Reinitialize plan a visit form using the form field values and proceed as new person
        /// </summary>
        private void ReInitializeAsNewPerson()
        {
            // reset person object since person changed
            InitializeVisitObject();

            // populate visit object
            _visit.AdultOneFirstName = tbAdultOneFirstName.Text;
            _visit.AdultOneLastName = tbAdultOneLastName.Text;
            _visit.AdultOneEmail = tbAdultOneEmail.Text;
            _visit.MobileNumber = tbAdultFormMobile.Text;
            _visit.AdultTwoFirstName = tbAdultTwoFirstName.Text;
            _visit.AdultTwoLastName = tbAdultTwoLastName.Text;
            _visit.CampusId = ddlCampus.SelectedItem.Value.AsInteger();
            _visit.CampusName = ddlCampus.SelectedItem.Text;
            _visit.VisitDate = DateTime.Parse( ddlVisitDate.SelectedValue );
            _visit.ServiceTime = ddlServiceTime.SelectedItem.Text;
            _visit.ServiceTimeScheduleId = ddlServiceTime.SelectedItem.Value.AsInteger();

            // sync form fields
            tbChildrenFormMobile.Text = tbAdultFormMobile.Text;
            tbChildrenFormMobile.Visible = tbChildrenFormMobile.Text.IsNullOrWhiteSpace();

            ddlEditCampus.SelectedValue = ddlCampus.SelectedValue;
            lblSubmitCampus.Text = ddlCampus.SelectedItem.Text;

            ddlEditVisitDate.SelectedValue = ddlVisitDate.SelectedValue;
            lblSubmitVisitDate.Text = ddlVisitDate.SelectedItem.Text;

            ddlEditServiceTime.SelectedValue = ddlServiceTime.SelectedValue;
            lblSubmitServiceTime.Text = ddlServiceTime.SelectedItem.Text;

            // ensure no kids are displaying on the form
            ltlExistingChildrenHorizontal.Text = "";
            ltlExistingChildrenVertical.Text = "";

            // set visit state
            _visit.MainPanel = MainPanel.Children;
            _visit.SubPanelAdults = SubPanelAdults.Form;
            _visit.SubPanelChildren = SubPanelChildren.Question;
            _visit.NoneRadioSelected = true;

            // set button state
            btnChildrenAddAnother.Visible = false;
            btnChildrenNext.Visible = false;

            ResetChildForm();
        }

        /// <summary>
        /// Reset Child Form to empty state
        /// </summary>
        private void ResetChildForm()
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
        }

        /// <summary>
        /// Check if all child form required inputs have values
        /// </summary>
        /// <returns></returns>
        private bool RequiredChildFormFieldsReady()
        {
            bool ready = true;

            if ( !ValidateFormField( tbChildrenFormMobile ) )
            {
                ready = false;
            }

            if ( !ValidateFormField( tbChildFirstName ) )
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
        /// Return true if all child form fields are blank
        /// </summary>
        /// <returns></returns>
        private bool AllChildFormFieldsEmpty()
        {
            return tbChildFirstName.Text.IsNullOrWhiteSpace() &&
                tbChildLastName.Text.IsNullOrWhiteSpace() &&
                ddlChildBdayYear.SelectedValue.IsNullOrWhiteSpace() &&
                ddlChildBdayMonth.SelectedValue.IsNullOrWhiteSpace() &&
                ddlChildBdayDay.SelectedValue.IsNullOrWhiteSpace();
        }

        /// <summary>
        /// Add child to child list and update kids names
        /// </summary>
        /// <param name="newChild"></param>
        private void AddChildToVisit( Child newChild )
        {
            _visit.Children.Add( newChild );

            string newChildName = String.Format( "<div class=\"existing-child\"><span>{0}</span ></div>", newChild.FirstName );
            ltlExistingChildrenHorizontal.Text += newChildName;
            ltlExistingChildrenVertical.Text += newChildName;
        }

        /// <summary>
        /// Create child object from form fields
        /// </summary>
        /// <returns></returns>
        private Child CreateChild()
        {
            Child newChild = new Child
            {
                FirstName = tbChildFirstName.Text,
                LastName = tbChildLastName.Text.IsNotNullOrWhitespace() ? tbChildLastName.Text : _visit.AdultOneLastName
            };

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
        /// Update form state
        /// </summary>
        private void UpdateFormState()
        {
            switch ( _visit.MainPanel )
            {
                case MainPanel.Adults:
                    btnProgressAdults.Enabled = false;
                    btnProgressChildren.Enabled = false;

                    divProgressChildren.Attributes["class"] = "step inactive";
                    divProgressSubmit.Attributes["class"] = "step inactive";
                    break;
                case MainPanel.Children:
                    btnProgressAdults.Enabled = true;
                    btnProgressChildren.Enabled = false;

                    divProgressChildren.Attributes["class"] = "step";
                    divProgressSubmit.Attributes["class"] = "step inactive";
                    break;
                case MainPanel.Submit:
                    btnProgressAdults.Enabled = true;
                    btnProgressChildren.Enabled = true;

                    divProgressChildren.Attributes["class"] = "step";
                    divProgressSubmit.Attributes["class"] = "step";
                    break;
                default:
                    break;
            }

            if ( ddlServiceTime.SelectedValue.IsNotNullOrWhitespace() )
            {
                divAdultTwo.Attributes["class"] = "";
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
        private void ResetVisitDropDowns( bool campus, bool visitDate, bool serviceTime )
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

                // disable next button
                btnAdultsNext.Enabled = false;
            }
        }

        /// <summary>
        /// Reset child bday dropdowns (adds a blank item after reset)
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        private void ResetBDayDropDowns( bool year, bool month, bool day )
        {
            // reset year
            if ( year )
            {
                ddlChildBdayYear.ClearSelection();
            }

            // reset month - force reset if year was reset
            if ( month || year )
            {
                ddlChildBdayMonth.Items.Clear();
                ddlChildBdayMonth.Items.Add( new ListItem( "", "" ) );

                ddlChildBdayMonth.Visible = false;
            }

            // reset day - force reset if year or month was reset
            if ( day || month || year )
            {
                ddlChildBdayDay.Items.Clear();
                ddlChildBdayDay.Items.Add( new ListItem( "", "" ) );

                ddlChildBdayDay.Visible = false;
            }
        }

        /// <summary>
        /// Mask a phone number
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        private string MaskPhoneNumber( string phoneNumber )
        {
            return String.Format( "(***) ***-{0}", phoneNumber.Substring( phoneNumber.Length - 4 ) );
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

            // loop through all sub domains
            for ( int i = 0; i < domainParts.Length; i++ )
            {
                maskedEmail += domainParts[i].Substring( 0, 1 );

                // add * for remaining characters
                for ( int j = 1; j < domainParts[i].Length; j++ )
                {
                    maskedEmail += "*";
                }

                // add dot except on final domain
                if ( i != domainParts.Length - 1 )
                {
                    maskedEmail += ".";
                }
            }

            return maskedEmail;
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
        private MainPanel? GetCurrentMainPanel( string dropDownListId )
        {
            switch ( dropDownListId )
            {
                case "ddlCampus":
                    return MainPanel.Adults;
                case "ddlVisitDate":
                    return MainPanel.Adults;
                case "ddlServiceTime":
                    return MainPanel.Adults;
                case "ddlEditCampus":
                    return MainPanel.Submit;
                case "ddlEditVisitDate":
                    return MainPanel.Submit;
                case "ddlEditServiceTime":
                    return MainPanel.Submit;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Return the relative panel control
        /// </summary>
        /// <param name="subPanelAdults"></param>
        /// <returns></returns>
        private Panel GetSubPanelControl( SubPanelAdults subPanelAdults )
        {
            switch ( subPanelAdults )
            {
                case SubPanelAdults.Existing:
                    return pnlAdultsExisting;
                case SubPanelAdults.Form:
                    return pnlAdultsForm;
                default:
                    return pnlAdultsForm;
            }
        }

        /// <summary>
        /// Return the relative panel control
        /// </summary>
        /// <param name="subPanelChildren"></param>
        /// <returns></returns>
        private Panel GetSubPanelControl( SubPanelChildren subPanelChildren )
        {
            switch ( subPanelChildren )
            {
                case SubPanelChildren.Existing:
                    return pnlChildrenExisting;
                case SubPanelChildren.Form:
                    return pnlChildrenForm;
                case SubPanelChildren.Question:
                    return pnlChildrenQuestion;
                default:
                    return pnlChildrenQuestion;
            }
        }

        /// <summary>
        /// Force Edit Details on Submit panel to View
        /// </summary>
        private void SetSubmitEditDetailsToView()
        {
            lblSubmitVisitDate.Visible = true;
            lblSubmitCampus.Visible = true;
            lblSubmitServiceTime.Visible = true;

            ddlEditVisitDate.Visible = false;
            ddlEditCampus.Visible = false;
            ddlEditServiceTime.Visible = false;

            btnEditVisitDetails.Text = "Edit details";
        }

        /// <summary>
        /// Save a person note to their profile
        /// </summary>
        /// <param name="rockContext"></param>
        /// <param name="entityId"></param>
        /// <param name="noteText"></param>
        private void SavePersonNote( int entityId, string noteText )
        {
            RockContext rockContext = new RockContext();

            NoteService noteService = new NoteService( rockContext );
            NoteTypeCache noteType = NoteTypeCache.Read( Rock.SystemGuid.NoteType.PERSON_TIMELINE_NOTE.AsGuid() );

            if ( noteType != null )
            {
                Note note = new Note
                {
                    NoteTypeId = noteType.Id,
                    IsSystem = false,
                    IsAlert = false,
                    IsPrivateNote = false,
                    EntityId = entityId,
                    Caption = string.Empty,
                    Text = noteText
                };

                noteService.Add( note );

                rockContext.SaveChanges();
            }
        }

        #endregion

        #endregion

        #region Helper Classes & Enums

        [Serializable]
        protected class Visit
        {
            // Visit Info
            public DateTime? VisitDate { get; set; }
            public int CampusId { get; set; }
            public string CampusName { get; set; }
            public string ServiceTime { get; set; }
            public int ServiceTimeScheduleId { get; set; }
            public string SurveyResponse { get; set; }

            // Adult One
            public string AdultOneFirstName { get; set; }
            public string AdultOneLastName { get; set; }
            public string AdultOneEmail { get; set; }
            public int AdultOnePersonId { get; set; }
            public int FamilyId { get; set; }
            public string MobileNumber { get; set; }

            // Adult Two
            public string AdultTwoFirstName { get; set; }
            public string AdultTwoLastName { get; set; }

            // Children
            public List<Child> Children { get; set; }

            // Form State
            public MainPanel MainPanel { get; set; }
            public SubPanelAdults SubPanelAdults { get; set; }
            public SubPanelChildren SubPanelChildren { get; set; }
            public bool NoneRadioSelected { get; set; }

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

        protected enum MainPanel
        {
            Adults,
            Children,
            Submit
        }

        protected enum SubPanelAdults
        {
            Existing,
            Form
        }

        protected enum SubPanelChildren
        {
            Existing,
            Form,
            Question
        }

        #endregion
    }
}