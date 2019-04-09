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
using Rock.Communication;
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
    [SystemEmailField( "Confirmation Email Template", "The system email to use to send the confirmation.", true, "", "", 4 )]

    public partial class ScheduleAVisit : RockBlock
    {
        const int AttributeId_Allergies = 676;
        const int AttributeId_HowDidYouHearAboutCCV = 719;
        const int DefinedTypeId_SourceofVisit = 33;

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

                WriteVisitToViewState();

                // bind dropdowns
                BindCampuses();

                // *********************************************** Probably Remove
                //BindStates();

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

            // if back button on submit page was pressed
            // ensure edit details is in corect state
            if ( button == btnSubmitBack )
            {
                SetSubmitEditDetailsToView();
            }

            Panel mainPanel = FindControl( button.CommandName ) as Panel;
            Panel subPanel = new Panel();

            switch ( mainPanel.ID )
            {
                case "pnlAdults":
                    // update _visit state
                    _visit.MainPanel = MainPanel.Adults;

                    // if back button on existing person form or Adults in progress navigation was hit we want to go to adult form panel
                    // else go to panel that is in _visit state
                    if ( button == btnAdultsExistingBack || button == btnProgressAdults)
                    {
                        subPanel = pnlAdultsForm;
                    }
                    else
                    {
                        subPanel = GetSubPanelControl( _visit.SubPanelAdults );
                    }

                    // reset children panel to existing or question - so they can cancel creating a child
                    if (_visit.Children.Count > 0 )
                    {
                        _visit.SubPanelChildren = SubPanelChildren.Existing;
                    }
                    else
                    {
                        _visit.SubPanelChildren = SubPanelChildren.Question;
                    }

                    break;
                case "pnlChildren":
                    // update _visit state
                    _visit.MainPanel = MainPanel.Children;

                    subPanel = GetSubPanelControl( _visit.SubPanelChildren );
                    break;
            }

            if ( mainPanel.IsNotNull() )
            {
                SetProgressButtonsState();

                ClearErrorNotifications();

                ShowFormPanel( mainPanel, subPanel );
            }

            WriteVisitToViewState();
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

                    // set submit label 
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

                    // set campusId in _visit and update viewstate
                    _visit.CampusId = campus.Id.ToString();
                    _visit.CampusName = campus.Name;
                    WriteVisitToViewState();


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

            // Pretty sure this needs to be &&

            if ( visitDateDropDownList.SelectedValue.IsNotNullOrWhitespace() || currentMainPanel != null || ddlCampus.SelectedValue.IsNotNullOrWhitespace() || day.IsNotNullOrWhitespace() )
            {
                if ( ddlCampus.SelectedValue.AsInteger() != 0 )
                {
                    CampusCache campus = CampusCache.Read( ddlCampus.SelectedValue.AsInteger() );

                    // set submit label
                    lblSubmitVisitDate.Text = visitDateDropDownList.SelectedItem.Text;

                    // sync value to other campus drop down
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
            MainPanel? currentMainPanel = GetCurrentMainPanel( rockDropDownList.ID );

            if ( rockDropDownList.SelectedValue.IsNotNullOrWhitespace() || currentMainPanel != null )
            {                    
                // sync value to other service time drop down
                switch ( currentMainPanel )
                {
                    case MainPanel.Adults:
                        ddlEditServiceTime.SelectedValue = rockDropDownList.SelectedValue;

                        // unhide spouse form fields
                        divSpouse.Attributes["class"] = "";
                        break;
                    case MainPanel.Submit:
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

            // first look for matching people
            PersonService.PersonMatchQuery searchParameters = new PersonService.PersonMatchQuery( 
                firstName: tbAdultFirstName.Text,
                lastName: tbAdultLastName.Text,
                email: tbAdultEmail.Text,
                mobilePhone: tbAdultFormMobile.Text );

            var personQry = personService.FindPersons( searchParameters, false, false );

            if ( personQry.Count() > 0)
            {
                // matches found

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
                    if ( _visit.PersonId == prsn.Id )
                    {
                        // since items added in order, the current person index should be 1 less than the count
                        selectedRadioIndex = rblExisting.Items.Count - 1;
                    }
                }
               
                // add none item
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
                // new person - populate visit object
                _visit.FirstName = tbAdultFirstName.Text;
                _visit.LastName = tbAdultLastName.Text;
                _visit.Email = tbAdultEmail.Text;

                // *************************************************************Probably remove
                //_visit.Address = tbAdultAddress.Text;
                //_visit.City = tbAdultCity.Text;
                //_visit.State = ddlAdultState.SelectedValue;
                //_visit.PostalCode = tbAdultPostalCode.Text;

                _visit.SpouseFirstName = tbSpouseFirstName.Text;
                _visit.SpouseLastName = tbSpouseLastName.Text;


                if ( tbAdultFormMobile.Text.IsNotNullOrWhitespace())
                {
                    _visit.MobileNumber = PhoneNumber.CleanNumber( tbAdultFormMobile.Text );

                    // update mobile field on children form
                    tbChildrenFormMobile.Text = tbAdultFormMobile.Text;
                }
            }

            // show next form panel
            if ( newPerson )
            {
                // set active tab to children
                _visit.MainPanel = MainPanel.Children;

                // get current subpanel
                Panel subPanel = GetSubPanelControl( _visit.SubPanelChildren );

                // show children panel
                ShowFormPanel( pnlChildren, subPanel );
            }
            else
            {
                // update visit state and show existing person panel
                _visit.SubPanelAdults = SubPanelAdults.Existing;

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

                    // if no mobile number was entered on adult form, get from person if it exists
                    string personMobileNumber = person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ).ToString();

                    if ( personMobileNumber.IsNotNullOrWhitespace() )
                    {
                        _visit.MobileNumber = personMobileNumber;
                        tbAdultFormMobile.Text = personMobileNumber;
                        tbChildrenFormMobile.Text = personMobileNumber;
                    }
                    // otherwise use mobile number from text box
                    else
                    {
                        _visit.MobileNumber = PhoneNumber.CleanNumber( tbAdultFormMobile.Text );
                    }

                    Person spouse = person.GetSpouse();

                    if ( spouse != null )
                    {
                        // populate spouse info
                        _visit.SpouseFirstName = spouse.FirstName;
                        _visit.SpouseLastName = spouse.LastName;
                    }

                    // dont add the kids if family ID is already in visit object - prevents kids added everytime back button is hit
                    if (_visit.FamilyId != person.GetFamily().Id)
                    {
                        _visit.FamilyId = person.GetFamily().Id;

                        // different family id - start with no children
                        _visit.Children.Clear();
                        ltlExistingChildrenHorizontal.Text = "";
                        ltlExistingChildrenVertical.Text = "";

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

                                    // set children subpanel to exising
                                    _visit.SubPanelChildren = SubPanelChildren.Existing;
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
                // reset visit object if none not already selected
                if (_visit.NoneRadioSelected == false)
                {
                    // reset person object since person changed
                    InitializeVisitObject();

                    // new person - populate visit object
                    _visit.FirstName = tbAdultFirstName.Text;
                    _visit.LastName = tbAdultLastName.Text;
                    _visit.Email = tbAdultEmail.Text;

                    _visit.SpouseFirstName = tbSpouseFirstName.Text;
                    _visit.SpouseLastName = tbSpouseLastName.Text;


                    // set visit state
                    _visit.MainPanel = MainPanel.Adults;
                    _visit.SubPanelAdults = SubPanelAdults.Existing;
                    _visit.SubPanelChildren = SubPanelChildren.Question;
                    _visit.NoneRadioSelected = true;

                    // set button state
                    btnChildrenAddAnother.Visible = false;
                    btnChildrenNext.Visible = false;
                    
                    ResetChildForm( true );
                }

                hasError = false;
            }

            if ( !hasError )
            {
                // Navigate to children panel
                _visit.MainPanel = MainPanel.Children;

                // get the subpanel to show
                Panel subPanel = GetSubPanelControl( _visit.SubPanelChildren );                    
 
                ShowFormPanel( pnlChildren, subPanel );

                nbAlertExisting.Text = "";
            }
            else
            {
                nbAlertExisting.NotificationBoxType = NotificationBoxType.Danger;
                nbAlertExisting.Text = "error";
            }

            WriteVisitToViewState();
        }

        #endregion

        #region Children Panel Events

        protected void ShowChildrenForm_Click( object sender, EventArgs e )
        {
            Button button = sender as Button;

            // default to children form
            Panel subPanel = pnlChildrenForm;

            if (button == btnNotMyChildren )
            {
                // if not children, set none radio selected so we dont reset the visit object prematurely
                _visit.NoneRadioSelected = true;

                // set adult panel to form panel - bypasses state issue, can circle back if we really need this to go back to existing panel
                _visit.SubPanelAdults = SubPanelAdults.Form;

                if (_visit.Children != null)
                {
                    // personclicked not my child, clear out children object
                    _visit.Children.Clear();
                    ltlExistingChildrenHorizontal.Text = "";
                    ltlExistingChildrenVertical.Text = "";
                }

                // set state and subpanel to children question panel
                _visit.SubPanelChildren = SubPanelChildren.Question;
                subPanel = pnlChildrenQuestion;

                btnChildrenAddAnother.Visible = false;
                btnChildrenNext.Visible = false;
            }
            else
            {
                // set state to children form
                _visit.SubPanelChildren = SubPanelChildren.Form;

                btnChildrenAddAnother.Visible = true;
                btnChildrenNext.Visible = true;
            }

            ShowFormPanel( pnlChildren, subPanel );

            WriteVisitToViewState();
        }

        protected void btnChildrenAddAnother_Click( object sender, EventArgs e )
        {
            // ensure required fields are coplete before proceeding
            if ( RequiredChildFormFieldsReady() )
            {
                // update mobile number in visit object if its different from text box
                if ( _visit.MobileNumber != PhoneNumber.CleanNumber( tbChildrenFormMobile.Text) )
                {
                    _visit.MobileNumber = PhoneNumber.CleanNumber( tbChildrenFormMobile.Text );
                    tbAdultFormMobile.Text = tbChildrenFormMobile.Text;
                }

                Child newChild = CreateChild();

                AddChildToVisit( newChild );

                // clear the form
                ResetChildForm( false );

                ClearErrorNotifications();

                WriteVisitToViewState();
            }
            else
            {
                // missing form fields, show error
                nbAlertChildForm.NotificationBoxType = NotificationBoxType.Danger;
                nbAlertChildForm.Text = "Please complete required fields above";
            }
        }

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

                // clear the form
                ResetChildForm( false );

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
                // set state and show submit panel
                _visit.MainPanel = MainPanel.Submit;

                ClearErrorNotifications();

                // change to submit
                ShowFormPanel( pnlSubmit, null );
            }

            WriteVisitToViewState();
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

            // enable / disable submit button
            if ( ddlEditCampus.Visible)
            {
                btnSubmitNext.Enabled = false;
            }
            else
            {
                btnSubmitNext.Enabled = true;
            }

        }

        protected void btnSubmitNext_Click( object sender, EventArgs e )
        {
            // disable and hide submit button so it cant be hit twice
            btnSubmitNext.Enabled = false;
            btnSubmitNext.Visible = false;

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
            var noteType = NoteTypeCache.Read( Rock.SystemGuid.NoteType.PERSON_TIMELINE_NOTE.AsGuid() );
            
            // service objects
            AttributeValueService attributeValueService = new AttributeValueService( rockContext );
            PersonService personService = new PersonService( rockContext );
            GroupService groupService = new GroupService( rockContext );
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );
            NoteService noteService = new NoteService( rockContext );
            
            // person / family objects
            Rock.Model.Group family = null;
            Person person = null;
            Person spouse = null;

            // use existing person if exists
            if ( _visit.PersonId > 0 )
            {
                person = personService.Get( _visit.PersonId );

                if ( person != null )
                {
                    family = person.GetFamily();

                    newFamily = false;
                }
            }

            // create new family if no existing family
            if ( newFamily )
            {
                person = new Person
                {
                    FirstName = _visit.FirstName,
                    LastName = _visit.LastName,
                    Email = _visit.Email,
                    ConnectionStatusValueId = connectionStatusValue.Id,
                    RecordTypeValueId = recordTypePersonId,
                    RecordStatusValueId = recordStatusValue.Id
                };

                family = PersonService.SaveNewPerson( person, rockContext, _visit.CampusId.AsInteger(), false );

                // add person note
                if ( noteType != null )
                {
                    Note note = new Note
                    {
                        NoteTypeId = noteType.Id,
                        IsSystem = false,
                        IsAlert = false,
                        IsPrivateNote = false,
                        EntityId = person.Id,
                        Caption = string.Empty,
                        Text = String.Format( "Visit planned by for {0} on {1} at the {2} campus", _visit.ServiceTime, _visit.VisitDate, _visit.CampusName )
                    };

                    noteService.Add( note );

                    rockContext.SaveChanges();
                }

                 // update visit object
                _visit.FamilyId = family.Id;
                _visit.PersonId = person.Id;
            }

            if ( family == null )
            {
                // something failed 
                // TODO: error handle and skip
                nbAlertSubmit.NotificationBoxType = NotificationBoxType.Danger;
                nbAlertSubmit.Text = "Error getting/creating family";

                return;
            }

            // we should have a person by now, update survey answer if they answered
            if ( rblSurvey.SelectedValue.IsNotNullOrWhitespace() )
            {
                DefinedValueService definedValueService = new DefinedValueService( rockContext );

                // get the DefinedValue that matches the survey answer
                DefinedValue surveyAnswer = definedValueService.Queryable().Where( a => a.DefinedTypeId == DefinedTypeId_SourceofVisit && a.Value == rblSurvey.SelectedValue ).SingleOrDefault();

                 // check to see if currently exists - to prevent duplicates if the attribute was created with person creation
                AttributeValue attributeValueSurvey = attributeValueService.Queryable().Where( av => av.EntityId == person.Id && av.AttributeId == AttributeId_HowDidYouHearAboutCCV ).SingleOrDefault();

                if ( attributeValueSurvey == null )
                {
                    // doesnt exist - create a new attribute value tied to the person, and of attribute type "HowDidYouHearAboutCCV"
                    attributeValueSurvey = new AttributeValue
                    {
                        EntityId = person.Id,
                        AttributeId = AttributeId_HowDidYouHearAboutCCV
                    };
                    attributeValueService.Add( attributeValueSurvey );
                }

                // update with the new value
                attributeValueSurvey.Value = surveyAnswer.Guid.ToString();

                _visit.Survey = rblSurvey.SelectedValue;
            }

            // if address specified add it to family ***************************************************** Probably Remove
            //if ( _visit.Address.IsNotNullOrWhitespace() && 
            //        _visit.City.IsNotNullOrWhitespace() &&
            //        _visit.State.IsNotNullOrWhitespace() &&
            //        _visit.PostalCode.IsNotNullOrWhitespace() )
            //{
            //    GroupService.AddNewGroupAddress( rockContext, family, homeLocationType.Guid.ToString(), _visit.Address, "", _visit.City, _visit.State, _visit.PostalCode, "US", true );

            //    rockContext.SaveChanges();
            //}

            // if mobile specified, add to person
            if ( _visit.MobileNumber.IsNotNullOrWhitespace() )
            {
                PhoneNumberService phoneNumberService = new PhoneNumberService( rockContext );

                PhoneNumber phoneNumber = phoneNumberService.Queryable()
                    .Where( a =>
                        a.PersonId == person.Id &&
                        a.NumberTypeValueId.HasValue &&
                        a.NumberTypeValueId.Value == mobilePhoneType.Id )
                    .FirstOrDefault();

                if ( phoneNumber == null )
                {
                    phoneNumber = new PhoneNumber
                    {
                        PersonId = person.Id,
                        NumberTypeValueId = mobilePhoneType.Id
                    };

                    phoneNumberService.Add( phoneNumber );
                }

                phoneNumber.Number = _visit.MobileNumber;
                phoneNumber.IsMessagingEnabled = true;

                rockContext.SaveChanges();
            }

            // if not a new family, check if spouse already in family
            if ( !newFamily && _visit.SpouseFirstName.IsNotNullOrWhitespace() )
            {
                GroupMember member = family.Members.FirstOrDefault( a => (a.Person.FirstName == _visit.SpouseFirstName || a.Person.NickName == _visit.SpouseFirstName) && a.GroupRoleId == adultRoleId );

                if ( member != null )
                {
                    spouse = personService.Get( member.Person.Guid );

                    // update visit object
                    _visit.SpouseFirstName = spouse.FirstName;
                    _visit.SpouseLastName = spouse.LastName;
                }
            }

            // if spouse not found, add new if specified
            if ( spouse == null && _visit.SpouseFirstName.IsNotNullOrWhitespace() )
            {
                spouse = new Person()
                {
                    FirstName = _visit.SpouseFirstName,
                    LastName = _visit.SpouseLastName.IsNotNullOrWhitespace() ? _visit.SpouseLastName : _visit.LastName,
                    ConnectionStatusValueId = connectionStatusValue.Id,
                    RecordTypeValueId = recordTypePersonId,
                    RecordStatusValueId = recordStatusValue.Id
                };

                GroupMember newFamilyMember = new GroupMember()
                {
                    Person = spouse,
                    Group = family,
                    GroupId = family.Id,
                    GroupRoleId = adultRoleId
                };

                groupMemberService.Add( newFamilyMember );

                rockContext.SaveChanges();

                // add person note for spouse
                if ( noteType != null )
                {
                    Note note = new Note
                    {
                        NoteTypeId = noteType.Id,
                        IsSystem = false,
                        IsAlert = false,
                        IsPrivateNote = false,
                        EntityId = spouse.Id,
                        Caption = string.Empty,
                        Text = String.Format( "Visit planned by for {0} on {1} at the {2} campus", _visit.ServiceTime, _visit.VisitDate, _visit.CampusName )
                    };

                    noteService.Add( note );
                }

                rockContext.SaveChanges();
            }

            // add children to family if specified
            if (_visit.Children.Count > 0 )
            {
                foreach ( var child in _visit.Children )
                {
                    // first check if already in family
                    GroupMember childFamilyMember = family.Members.FirstOrDefault( a => (a.Person.FirstName == child.FirstName || a.Person.NickName == child.FirstName) && a.Person.BirthDate == child.Birthday );

                    if ( childFamilyMember == null)
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

                        GroupMember newFamilyMember = new GroupMember()
                        {
                            Person = newChild,
                            Group = family,
                            GroupId = family.Id,
                            GroupRoleId = childRoleId
                        };

                        groupMemberService.Add( newFamilyMember );

                        rockContext.SaveChanges();

                        // add person note to child
                        if ( noteType != null )
                        {
                            Note note = new Note
                            {
                                NoteTypeId = noteType.Id,
                                IsSystem = false,
                                IsAlert = false,
                                IsPrivateNote = false,
                                EntityId = newChild.Id,
                                Caption = string.Empty,
                                Text = String.Format( "Visit planned by for {0} on {1} at the {2} campus", _visit.ServiceTime, _visit.VisitDate, _visit.CampusName )
                            };

                            noteService.Add( note );
                        }

                        rockContext.SaveChanges();

                        // update optional attributes (only doing this for new chidren added to family)
                        if ( newFamilyMember != null && ( child.Allergies.IsNotNullOrWhitespace() || child.Gender != Gender.Unknown || child.Grade.IsNotNullOrWhitespace() ) )
                        {
                            Person childPerson = newFamilyMember.Person;

                            childPerson.SetBirthDate( child.Birthday );

                            if ( child.Allergies.IsNotNullOrWhitespace() )
                            {
                                // check to see if value currently exists - to prevent duplicates if the attribute was created with person creation
                                AttributeValue avAllergy = attributeValueService.Queryable().Where( av => av.EntityId == childPerson.Id && av.AttributeId == AttributeId_Allergies ).SingleOrDefault();

                                if ( avAllergy == null )
                                {
                                    // doesnt exist - create a new attribute value tied to the person(child), and of attribute type "Allergy"
                                    avAllergy = new AttributeValue
                                    {
                                        EntityId = childPerson.Id,
                                        AttributeId = AttributeId_Allergies
                                    };
                                    attributeValueService.Add( avAllergy );
                                }

                                // update with the new value
                                avAllergy.Value = child.Allergies;
                            }

                            if ( child.Gender != Gender.Unknown )
                            {
                                childPerson.Gender = child.Gender;
                            }

                            if ( child.Grade.IsNotNullOrWhitespace() )
                            {
                                childPerson.GradeOffset = child.Grade.AsInteger();
                            }
                        }

                        rockContext.SaveChanges();
                    }
                }
            }

            // Send email confirmation
            Guid confirmationEmailTemplateGuid = Guid.Empty;

            if ( !Guid.TryParse( GetAttributeValue( "ConfirmationEmailTemplate" ), out confirmationEmailTemplateGuid ) )
            {
                confirmationEmailTemplateGuid = Guid.Empty;
            }

            if ( confirmationEmailTemplateGuid != Guid.Empty )
            {
                // Build merge fields
                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null );

                mergeFields.Add( "VisitPerson", person );
                mergeFields.Add( "VisitCampus", _visit.CampusName );
                mergeFields.Add( "VisitDate", _visit.VisitDate );
                mergeFields.Add( "VisitService", _visit.ServiceTime );

                // Send confirmation email
                var publicApplicationRoot = Rock.Web.Cache.GlobalAttributesCache.Read( rockContext ).GetValue( "PublicApplicationRoot" );

                var recipient = new List<RecipientData>
                {
                    new RecipientData( person.Email, mergeFields )
                };

                var emailMessage = new RockEmailMessage( confirmationEmailTemplateGuid );
                emailMessage.AddRecipient( new RecipientData( person.Email, mergeFields ) );
                emailMessage.AppRoot = ResolveRockUrl( "~/" );
                emailMessage.ThemeRoot = ResolveRockUrl( "~~/" );
                emailMessage.CreateCommunicationRecord = true;
                emailMessage.Send();
            }
                       
            // Trigger workflow to add person to group

            // change to success panel
            ShowFormPanel( pnlSuccess, null );

            WriteVisitToViewState();
        }

        protected void btnSubmitRetry_Click( object sender, EventArgs e )
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

        //   **************************************************************************** Probably Remove
        /// <summary>
        /// Bind states dropdown
        /// </summary>
        //private void BindStates()
        //{
        //    // hardcoding us guid for now
        //    string usGuid = "F4DAEB01-A0E5-426A-A425-7F6D21DF1CE7";

        //    // get states from defined type
        //    var statesDefinedType = DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.LOCATION_ADDRESS_STATE ) );

        //    var stateList = statesDefinedType
        //        .DefinedValues
        //        .Where( v =>
        //            (
        //                v.AttributeValues.ContainsKey( "Country" ) &&
        //                v.AttributeValues["Country"] != null &&
        //                v.AttributeValues["Country"].Value.Equals( usGuid, StringComparison.OrdinalIgnoreCase )
        //            ) )
        //        .OrderBy( v => v.Order )
        //        .ThenBy( v => v.Value )
        //        .Select( v => new { Id = v.Value, Value = v.Description } )
        //        .ToList();

        //    // add states to drop down
        //    if ( stateList.Any() )
        //    {
        //        ddlAdultState.Items.Clear();

        //        ddlAdultState.Items.Add( new ListItem( "" ) );

        //        foreach ( var state in stateList )
        //        {
        //            ddlAdultState.Items.Add( new ListItem( state.Value, state.Value ) );
        //        }
        //    }
        //}

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

            // set nav button states based off subpanel
            if ( subPanel == pnlChildrenForm || subPanel == pnlChildrenExisting )
            {
                // show children next button
                btnChildrenNext.Visible = true;
            }

            if ( subPanel == pnlChildrenExisting )
            {
                // hide add another child button
                btnChildrenAddAnother.Visible = false;
            }

            SetProgressButtonsState();
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
        /// Reset Child Form to empty state
        /// </summary>
        /// <param name="resetMobileNumber"></param>
        private void ResetChildForm( bool resetMobileNumber )
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

            if ( resetMobileNumber )
            {
                tbAdultFormMobile.Text = "";
                tbChildrenFormMobile.Text = "";
            }

        }

        /// <summary>
        /// Initialize blank visit object
        /// </summary>
        private void InitializeVisitObject()
        {
            _visit = new Visit();
            _visit.Children = new List<Child>();

            // -1 indicates a new person needs to be created
            _visit.PersonId = -1;

            // ensure no kids are displaying on the form
            ltlExistingChildrenHorizontal.Text = "";
            ltlExistingChildrenVertical.Text = "";
        }

        /// <summary>
        /// Write Visit to View State
        /// </summary>
        private void WriteVisitToViewState()
        {
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
            switch ( _visit.MainPanel )
            {
                case MainPanel.Adults:
                    btnProgressAdults.Enabled = false;
                    btnProgressChildren.Enabled = false;
                    break;
                case MainPanel.Children:
                    btnProgressAdults.Enabled = true;
                    btnProgressChildren.Enabled = false;

                    break;
                case MainPanel.Submit:
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

            // TODO
            // set state of children form
            //if ( _visit.SubPanelChildren )
            //{
            //    btnChildrenAddAnother.Visible = true;
            //    btnChildrenNext.Visible = true;
            //}
            //else
            //{
            //    btnChildrenAddAnother.Visible = false;
            //    btnChildrenNext.Visible = false;
            //}               
        }

        /// <summary>
        /// Restore progress buttons enabled state
        /// </summary>
        private void SetProgressButtonsState()
        {
            switch ( _visit.MainPanel )
            {
                case MainPanel.Adults:
                    btnProgressAdults.Enabled = false;
                    btnProgressChildren.Enabled = false;
                    break;
                case MainPanel.Children:
                    btnProgressAdults.Enabled = true;

                    btnProgressChildren.Enabled = false;
                    btnProgressChildren.Attributes.Add( "class", "active" );

                    break;
                case MainPanel.Submit:
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

                ddlChildBdayMonth.Visible = false;
            }

            // reset day - force reset if year or month was reset
            if (day || month || year)
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
        /// Check if all child form required inputs have values
        /// </summary>
        /// <returns></returns>
        private bool RequiredChildFormFieldsReady( )
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
        /// Add child to child list and update kids names
        /// </summary>
        /// <param name="newChild"></param>
        private void AddChildToVisit( Child newChild )
        {
            _visit.Children.Add( newChild );

            // add new child to existing children displayed
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
            // build new child object
            Child newChild = new Child
            {
                FirstName = tbChildFirstName.Text,
                LastName = tbChildLastName.Text.IsNotNullOrWhitespace() ? tbChildLastName.Text : _visit.LastName
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

        #endregion

        #endregion

        #region Helper Class
        [Serializable]
        protected class Visit
        {
            // Visit Info
            public string VisitDate { get; set; }
            public string CampusId { get; set; }
            public string CampusName { get; set; }
            public string ServiceTime { get; set; }
            public string Survey { get; set; }

            // Adult
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }

            public int PersonId { get; set; }
            public int FamilyId { get; set; }


            public string MobileNumber { get; set; }
            //public string Address { get; set; }
            //public string City { get; set; }
            //public string State { get; set; }
            //public string PostalCode { get; set; }

            // Spouse
            public string SpouseFirstName { get; set; }
            public string SpouseLastName { get; set; }

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

        #endregion

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


    }
}