using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
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
                // temp - need values
                ddlServiceTime.Items.Add( new ListItem( "1", "1" ) );
                ddlServiceTime.Items.Add( new ListItem( "2", "2" ) );

            }

            Person person = (Person)ViewState["VisitPerson"];

            if ( person != null )
            {
                tbChildFirstName.Text = person.FirstName;
            }
        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {

        }

        protected void dppChildBDay_SelectedDatePartsChanged( object sender, EventArgs e )
        {

        }

        protected void btnAdultsNext_Click( object sender, EventArgs e )
        {
            // try to load visit from viewstate
            if ( !LoadVisit() )
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

            _visit.VisitDate = dpVisitDate.Text;
            _visit.CampusId = cpCampus.SelectedValue;
            _visit.ServiceTime = ddlServiceTime.SelectedValue;

            _visit.SpouseFirstName = tbSpouseFirstName.Text;
            _visit.SpouseLastName = tbSpouseLastName.Text;

            // write object to viewstate
            ViewState["Visit"] = _visit;

            // change to children panel
            pnlAdults.Visible = false;
            pnlChildren.Visible = true;
        }


        protected void btnChildrenBack_Click( object sender, EventArgs e )
        {


            // change to adults panel
            pnlChildren.Visible = false;
            pnlAdults.Visible = true;
        }

        protected void btnChildrenNext_Click( object sender, EventArgs e )
        {


            // change to submit
            pnlChildren.Visible = false;
            pnlSubmit.Visible = true;
        }

        protected void btnChildrenAddAnother_Click( object sender, EventArgs e )
        {

        }





        protected void btnSubmitBack_Click( object sender, EventArgs e )
        {

            // change to children panel
            pnlSubmit.Visible = false;
            pnlChildren.Visible = true;
        }

        protected void btnSubmitNext_Click( object sender, EventArgs e )
        {



            // change to success panel
            pnlForm.Visible = false;
            pnlSuccess.Visible = true;
        }

        protected void btnProgressAdults_Click( object sender, EventArgs e )
        {

        }

        protected void btnProgressChildren_Click( object sender, EventArgs e )
        {

        }

        #region Methods

        /// <summary>
        /// Load Visit from View State
        /// </summary>
        /// <returns></returns>
        private bool LoadVisit()
        {
            _visit = (Visit)ViewState["Visit"];

            if (_visit != null)
            {
                return true;
            }

            return false;
        }

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
            public List<Child> Children { get; set; }
                       
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
    }
}