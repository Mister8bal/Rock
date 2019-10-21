// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.church_ccv.Communication
{
    /// <summary>
    /// CCV Custom Email Unsubscribe block
    /// </summary>
    [DisplayName( "Email Unsubscribe" )]
    [Category( "CCV > Communication" )]
    [Description( "Allows user to unsubscribe from marketing categories or all communications" )]

    [TextField( "Reasons to Exclude", "A delimited list of the Inactive Reasons to exclude from Reason list", false, "No Activity,Deceased", "",0 )]
    public partial class EmailUnsubscribe : RockBlock
    {
        #region Fields

        private const int _attributeId_MarketingOptOut = 96313;
        private Person _person = null;

        #endregion

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            var rockContext = new RockContext();

            var key = PageParameter( "Person" );
            if ( !string.IsNullOrWhiteSpace( key ) )
            {
                var personService = new PersonService( rockContext );
                _person = personService.GetByUrlEncodedKey( key );
            }

            if ( _person == null && CurrentPerson != null )
            {
                _person = CurrentPerson;
            }

            if ( _person == null )
            {
                hSuccessTitle.InnerHtml = "Failed To Load";
                pSuccessContent.InnerHtml = "Unfortunately, we're unable to update your email preference, as we're not sure who you are. Please login.";

                divSettings.Visible = false;
                btnSubmit.Visible = false;
                divSuccess.Visible = true;
            }

            var excludeReasons = GetAttributeValue( "ReasonstoExclude" ).SplitDelimitedValues( false ).ToList();
            var inactiveReasons = DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.PERSON_RECORD_STATUS_REASON.AsGuid() ).DefinedValues
                .Where( v => !excludeReasons.Contains( v.Value, StringComparer.OrdinalIgnoreCase ) )
                .Select( v => new
                {
                    Name = v.Value,
                    v.Id
                } );

            ddlInactiveReason.SelectedIndex = -1;
            ddlInactiveReason.DataSource = inactiveReasons;
            ddlInactiveReason.DataTextField = "Name";
            ddlInactiveReason.DataValueField = "Id";
            ddlInactiveReason.DataBind();        
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
                if ( _person != null )
                {
                    // check for any categories the person has already opted out of
                    var marketingOptOut = new AttributeValueService( new RockContext() ).GetByAttributeIdAndEntityId( _attributeId_MarketingOptOut, _person.Id );

                    if ( marketingOptOut != null && marketingOptOut.Value.IsNotNullOrWhitespace() )
                    {
                        // opt out categories found, update the selected options
                        cbGeneral.Checked = !marketingOptOut.Value.Contains( "General" );
                        cbNextGen.Checked = !marketingOptOut.Value.Contains( "Next Gen" );
                        cbSummerCamps.Checked = !marketingOptOut.Value.Contains( "Summer Camps" );
                        cbStars.Checked = !marketingOptOut.Value.Contains( "Stars Sports" );
                        cbSpecialNeeds.Checked = !marketingOptOut.Value.Contains( "Special Needs" );
                        cbMissions.Checked = !marketingOptOut.Value.Contains( "Missions" );
                        cbMusic.Checked = !marketingOptOut.Value.Contains( "CCV Music" );
                    }

                    // set the radio button selection if the person has already opted out of emails
                    switch ( _person.EmailPreference )
                    {
                        case EmailPreference.NoMassEmails:
                        case EmailPreference.DoNotEmail:
                            {
                                if ( _person.RecordStatusValueId != DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE ).Id )
                                {
                                    if ( rbEmailPreferenceDoNotEmail.Visible )
                                    {
                                        rbEmailPreferenceDoNotEmail.Checked = true;
                                    }
                                }
                                break;
                            }
                    }
                }
            }
        }

        #endregion

        #region Edit Events

        /// <summary>
        /// Handles the Click event of the btnSubmit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSubmit_Click( object sender, EventArgs e )
        {
            if ( _person != null )
            {
                var rockContext = new RockContext();
                var avService = new AttributeValueService( rockContext );
                var personService = new PersonService( rockContext );
                var person = personService.Get( _person.Id );

                if ( person != null )
                {
                    // set the persons email preference
                    if ( rbEmailPreferenceDoNotEmail.Checked )
                    {
                        person.EmailPreference = EmailPreference.NoMassEmails;
                    }
                    else if ( rbNotInvolved.Checked )
                    {
                        person.EmailPreference = EmailPreference.DoNotEmail;
                    }
                    else
                    {
                        person.EmailPreference = EmailPreference.EmailAllowed;
                    }

                    // update the persons's marketing opt out
                    // i think its beneficial to always update the selections regardless of the radio button option selected
                    string newOptOutPreference = GetOptOutSelections();

                    AttributeValue marketingOptOut = avService.GetByAttributeIdAndEntityId(_attributeId_MarketingOptOut, person.Id);

                    if ( marketingOptOut == null )
                    {
                        // attribute doesnt exist for person, create a new attribute
                        marketingOptOut = new AttributeValue
                        {
                            EntityId = person.Id,
                            AttributeId = _attributeId_MarketingOptOut
                        };
                        avService.Add( marketingOptOut );
                    }

                    marketingOptOut.Value = newOptOutPreference;

                    // set person status                    
                    if ( rbNotInvolved.Checked )
                    {
                        var inactiveRecordStatus = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE );
                        if ( inactiveRecordStatus != null )
                        {
                            person.RecordStatusValueId = inactiveRecordStatus.Id;
                        }

                        var inactiveReason = DefinedValueCache.Read( ddlInactiveReason.SelectedValue.AsInteger() );
                        if ( inactiveReason != null )
                        {
                            person.RecordStatusReasonValueId = inactiveReason.Id;
                        }

                        var reviewReason = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_REVIEW_REASON_SELF_INACTIVATED );
                        if ( reviewReason != null )
                        {
                            person.ReviewReasonValueId = reviewReason.Id;
                        }

                        // If the inactive reason note is the same as the current review reason note, update it also.
                        if ( ( person.InactiveReasonNote ?? string.Empty ) == ( person.ReviewReasonNote ?? string.Empty ) )
                        {
                            person.InactiveReasonNote = tbInactiveNote.Text;
                        }

                        person.ReviewReasonNote = tbInactiveNote.Text;
                    }
                    else
                    {
                        var activeRecordStatus = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE );
                        if ( activeRecordStatus != null )
                        {
                            person.RecordStatusValueId = activeRecordStatus.Id;
                        }

                        person.RecordStatusReasonValueId = null;
                    }

                    rockContext.SaveChanges();

                    divSettings.Visible = false;
                    divSuccess.Visible = true;

                    return;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Create a comma delimitted string from the category check boxes
        /// </summary>
        /// <returns></returns>
        private string GetOptOutSelections()
        {
            string returnString = "";
            List<string> optOutSelections = new List<string>();

            if ( !cbGeneral.Checked )
            {
                optOutSelections.Add( "General" );
            }

            if ( !cbNextGen.Checked )
            {
                optOutSelections.Add( "Next Gen" );
            }

            if ( !cbSummerCamps.Checked )
            {
                optOutSelections.Add( "Summer Camps" );
            }

            if ( !cbStars.Checked )
            {
                optOutSelections.Add( "Stars Sports" );
            }

            if ( !cbSpecialNeeds.Checked )
            {
                optOutSelections.Add( "Special Needs" );
            }

            if ( !cbMissions.Checked )
            {
                optOutSelections.Add( "Missions" );
            }

            if ( !cbMusic.Checked )
            {
                optOutSelections.Add( "CCV Music" );
            }

            if ( optOutSelections.Count > 0 )
            {
                for ( int i = 0; i < optOutSelections.Count; i++ )
                {
                    if ( i != 0 )
                    {
                        returnString += ",";
                    }

                    returnString += optOutSelections[i];
                }
            }

            return returnString;
        }

        #endregion
    }
}