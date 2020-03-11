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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.church_ccv.Security
{
    [DisplayName( "CCV Captive Portal Form" )]
    [Category( "CCV > Security" )]
    [Description( "Form used to access to Wi-Fi. Customized for CCV specifically to capture new people." )]

    #region Block Settings
    [CodeEditorField(
        name: "Welcome Content",
        description: "Content displayed on the first panel of the captive portal",
        defaultValue: "Welcome to CCV! We're so glad you're here. Log on to our network once and this device will automatically connect at any of our campuses.",
        category: "Captive Portal Content",
        order: 0,
        key: "WelcomeContent" )]
    [TextField(
        name: "Title Text",
        description: "Title Text to display",
        defaultValue: "Thank you for joining us.",
        category: "Captive Portal Content",
        order: 1,
        key: "TitleText" )]
    [TextField(
        name: "Button Text",
        description: "Text to display on the button",
        defaultValue: "Connect",
        category: "Captive Portal Content",
        order: 2,
        key: "ButtonText" )]
    [TextField(
        name: "MAC Address Paramameter",
        description: "The query string parameter used for the MAC Address",
        defaultValue: "client_mac",
        category: "Captive Portal Settings",
        order: 5,
        key: "MacAddressParam" )]
    [TextField(
        name: "Location Parameter",
        description: "The query string parameter used for the access point location",
        defaultValue: "loc",
        category: "Captive Portal Settings",
        order: 6,
        key: "LocationParam")]
    [TextField(
        name: "Release Url",
        description: "The full URL to redirect users to after registration.",
        category: "Captive Portal Settings",
        order: 7,
        key: "ReleaseUrl" )]
    [BooleanField(
        name: "Show Mobile Phone",
        description: "Show or hide the Mobile Phone Number field. If it is visible then it will be required.",
        defaultValue: true,
        category: "Captive Portal Settings",
        order: 11,
        key: "ShowMobilePhone",
        IsRequired = true )]
    [BooleanField(
        name: "Show Email",
        description: "Show or hide the Email field. If it is visible then it will be required.",
        defaultValue: true,
        category: "Captive Portal Settings",
        order: 12,
        key: "ShowEmail",
        IsRequired = true )]
    [DefinedValueField(
        definedTypeGuid: Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON,
        name: "New Person Record Type",
        description: "The person type to assign to new persons created by Captive Portal.",
        defaultValue: Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON,
        category: "Captive Portal Settings",
        order: 13,
        key: "NewPersonRecordType" )]
    [DefinedValueField(
        definedTypeGuid: Rock.SystemGuid.DefinedType.PERSON_RECORD_STATUS,
        name: "New Person Record Status",
        description: "The record status to assign to new persons created by Captive Portal.",
        defaultValue: Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE,
        category: "Captive Portal Settings",
        order: 14,
        key: "NewPersonRecordStatus" )]
    [DefinedValueField(
        definedTypeGuid: Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS,
        name: "New Person Connection Status",
        description: "The connection status to assign to new persons created by Captive Portal",
        defaultValue: Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_VISITOR,
        category: "Captive Portal Settings",
        order: 15,
        key: "NewPersonConnectionStatus" )]    
    #endregion Block Settings
    public partial class CCVCaptivePortalForm : RockBlock
    {
        /// <summary>
        /// The user agents to ignore. UA strings that begin with one of these will be ignored.
        /// This is to fix Apple devices loading the page with its CaptiveNetwork WISPr UA and messing
        /// up the device info, which is parsed from the UA. Ignoring "CaptiveNetworkSupport*"
        /// will fix 100% of current known issues, if more than a few come up we should put this
        /// into the DB as DefinedType/DefinedValues.
        /// </summary>
        private List<string> _userAgentsToIgnore = new List<string>()
        {
            "CaptiveNetworkSupport"
        };

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            nbAlert.Visible = false;

            // Go through the UA ignore list and don't load anything we don't care about or want.
            foreach ( string userAgentToIgnore in _userAgentsToIgnore )
            {
                if ( Request.UserAgent.StartsWith( userAgentToIgnore ) )
                {
                    return;
                }
            }

            if ( !IsPostBack )
            {
                string macAddress = RockPage.PageParameter( GetAttributeValue( "MacAddressParam" ) );
                if ( string.IsNullOrWhiteSpace( macAddress ) || !macAddress.IsValidMacAddress() )
                {
                    nbAlert.Text = "Missing or invalid MAC Address";
                    nbAlert.Visible = true;
                    pnlDetails.Visible = false;
                    return;
                }

                string releaseUrl = GetAttributeValue( "ReleaseUrl" );
                if ( string.IsNullOrWhiteSpace( releaseUrl ) || !releaseUrl.IsValidUrl() )
                {
                    nbAlert.Text = "Missing or invalid Release Url";
                    nbAlert.Visible = true;
                    pnlDetails.Visible = false;
                    return;
                }

                // Save the supplied MAC address to the page removing any non-Alphanumeric characters
                macAddress = macAddress.RemoveAllNonAlphaNumericCharacters();
                hfMacAddress.Value = macAddress;

                // check for existing device
                RockContext rockContext = new RockContext();
                PersonalDeviceService personalDeviceService = new PersonalDeviceService( rockContext );
                PersonalDevice personalDevice = personalDeviceService.GetByMACAddress( macAddress );

                // create device if doesnt exist
                if ( personalDevice == null )
                {
                    personalDevice = new PersonalDevice()
                    {
                        MACAddress = macAddress
                    };

                    personalDeviceService.Add( personalDevice );
                    rockContext.SaveChanges();
                }

                // Update the device info
                UAParser.ClientInfo client = UAParser.Parser.GetDefault().Parse( Request.UserAgent );
                personalDevice.PersonalDeviceTypeValueId = GetDeviceTypeValueId();
                personalDevice.PlatformValueId = GetDevicePlatformValueId( client );
                personalDevice.DeviceVersion = GetDeviceOsVersion( client );
                rockContext.SaveChanges();

                // check if the device is already linked to a person
                if ( personalDevice.PersonAlias != null )
                {
                    Response.Redirect( CreateRedirectUrl( personalDevice.PersonAlias.Id ) );
                    return;
                }

                // check if user is logged in
                if ( CurrentPerson != null )
                {
                    // link the CurrentPerson to the device and skip form
                    RockPage.LinkPersonAliasToDevice( CurrentPerson.PrimaryAlias.Id, macAddress );

                    Response.Redirect( CreateRedirectUrl( CurrentPerson.PrimaryAlias.Id ) );
                    return;
                }

                // We are going to create this everytime they hit the captive portal page. 
                // Otherwise if the device is saved but not linked to an actual user (not the fake one created here)
                // and then deleted by the user/browser/software, then they'll never get the cookie again and won't automatically get linked by RockPage.
                CreateDeviceCookie( macAddress );

                // check for access point location and then look for a matching campus
                string locationParam = RockPage.PageParameter( GetAttributeValue( "LocationParam" ) );
                int? campusId = GetCampusIdFromLocation( locationParam );
                if ( campusId != null )
                {
                    // save campus id to the page
                    hfCampusId.Value = campusId.ToString();
                }

                // Set the title text
                string titleText = GetAttributeValue( "TitleText" );
                if ( titleText.IsNotNullOrWhiteSpace() )
                {
                    lblTitleText.Text = titleText;
                }

                // Set the welcome content
                string welcomeContent = GetAttributeValue( "WelcomeContent" );
                if ( welcomeContent.IsNotNullOrWhitespace() )
                {
                    lWelcomeContent.Text = welcomeContent;
                }

                // Set mobile phone form field
                bool showMobilePhone = GetAttributeValue( "ShowMobilePhone" ).AsBoolean();
                tbMobilePhone.Visible = showMobilePhone;
                tbMobilePhone.Required = showMobilePhone;
                tbMobilePhone.Enabled = showMobilePhone;

                // Set email form field
                bool showEmail = GetAttributeValue( "ShowEmail" ).AsBoolean();
                tbEmail.Visible = showEmail;
                tbEmail.Required = showEmail;
                tbEmail.Enabled = showEmail;

                // Set connect button text
                string connectButtonText = GetAttributeValue( "ButtonText " );
                btnConnect.Text = connectButtonText.IsNotNullOrWhiteSpace() ? connectButtonText : "Connect";
            }
        }

        /// <summary>
        /// Uses the Request information to determine if the device is mobile or not
        /// </summary>
        /// <returns>DevinedValueId for "Mobile" or "Computer", Mobile includes Tablet. Null if there is a data issue and the DefinedType is missing</returns>
        private int? GetDeviceTypeValueId()
        {
            // Get the device type Mobile or Computer
            DefinedTypeCache definedTypeCache = DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.PERSONAL_DEVICE_TYPE.AsGuid() );
            DefinedValueCache definedValueCache = null;

            var clientType = InteractionDeviceType.GetClientType( Request.UserAgent );
            clientType = clientType == "Mobile" || clientType == "Tablet" ? "Mobile" : "Computer";

            if ( definedTypeCache != null )
            {
                definedValueCache = definedTypeCache.DefinedValues.FirstOrDefault( v => v.Value == clientType );

                if ( definedValueCache == null )
                {
                    definedValueCache = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSONAL_DEVICE_TYPE_COMPUTER.AsGuid() );
                }

                return definedValueCache.Id;
            }

            return null;
        }

        /// <summary>
        /// Parses ClientInfo to find the OS family
        /// </summary>
        /// <param name="client">The client.</param>
        /// <returns>DefinedValueId for the found OS. Uses "Other" if the OS is not in DefinedValue. Null if there is a data issue and the DefinedType is missing</returns>
        private int? GetDevicePlatformValueId( UAParser.ClientInfo client )
        {
            // get the OS
            string platform = client.OS.Family.Split( ' ' ).First();

            DefinedTypeCache definedTypeCache = DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.PERSONAL_DEVICE_PLATFORM.AsGuid() );
            DefinedValueCache definedValueCache = null;
            if ( definedTypeCache != null )
            {
                definedValueCache = definedTypeCache.DefinedValues.FirstOrDefault( v => v.Value == platform );

                if ( definedValueCache == null )
                {
                    definedValueCache = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSONAL_DEVICE_PLATFORM_OTHER.AsGuid() );
                }

                return definedValueCache.Id;
            }

            return null;
        }

        /// <summary>
        /// Parses ClientInfo and gets the device os version. If it cannot be determined returns the OS family string without the platform
        /// </summary>
        /// <param name="client">The client.</param>
        /// <returns></returns>
        private string GetDeviceOsVersion( UAParser.ClientInfo client )
        {
            if ( client.OS.Major == null )
            {
                string platform = client.OS.Family.Split( ' ' ).First();
                return client.OS.Family.Replace( platform, string.Empty ).Trim();
            }

            return string.Format(
                "{0}.{1}.{2}.{3}",
                client.OS.Major ?? "0",
                client.OS.Minor ?? "0",
                client.OS.Patch ?? "0",
                client.OS.PatchMinor ?? "0" );
        }

        /// <summary>
        /// Returns a campus if match found in location parameter.
        /// </summary>
        /// <param name="locationParam"></param>
        /// <returns></returns>
        private int? GetCampusIdFromLocation( string location )
        {
            var campuses = CampusCache.All();

            foreach ( var campus in campuses )
            {
                // check the location for an active campus name or shortcode
                if ( location.IsNotNullOrWhiteSpace() && 
                     campus.IsActive == true && 
                     campus.Name.IsNotNullOrWhiteSpace() &&
                     campus.ShortCode.IsNotNullOrWhiteSpace() &&
                     ( location.Contains( campus.Name ) || location.Contains( campus.ShortCode ) ) )
                {
                    return campus.Id;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates the device cookie if it does not exist.
        /// </summary>
        private void CreateDeviceCookie( string macAddress )
        {
            if ( Request.Cookies["rock_wifi"] == null )
            {
                HttpCookie httpcookie = new HttpCookie( "rock_wifi" );
                httpcookie.Expires = DateTime.MaxValue;
                httpcookie.Values.Add( "ROCK_PERSONALDEVICE_ADDRESS", macAddress );
                Response.Cookies.Add( httpcookie );
            }
        }

        /// <summary>
        /// Handles the Click event of the btnConnect control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnConnect_Click( object sender, EventArgs e )
        {
            // link the device in the hidden field to person
            int? primaryAliasId = LinkDeviceToPerson();
            if ( primaryAliasId != null )
            {
                Response.Redirect( CreateRedirectUrl( primaryAliasId ) );
                return;
            }

            // Send them back to Front Porch without user info
            Response.Redirect( CreateRedirectUrl( null ) );
        }

        /// <summary>
        /// Try to link the device to a person and return the primary alias ID if successful
        /// </summary>
        /// <returns>true if device successfully linked to a person</returns>
        protected int? LinkDeviceToPerson()
        {
            // So lets try to find the user using entered info and then link them to the device.
            PersonService personService = new PersonService( new RockContext() );
            int mobilePhoneTypeId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE ).Id;
            Person person = null;
            string mobilePhoneNumber = string.Empty;

            // Look for a single match if we ask for phone or email
            if ( tbMobilePhone.Visible || tbEmail.Visible )
            {
                if ( tbMobilePhone.Visible )
                {
                    mobilePhoneNumber = tbMobilePhone.Text.RemoveAllNonAlphaNumericCharacters();
                }

                var personQuery = new PersonService.PersonMatchQuery( tbFirstName.Text, tbLastName.Text, tbEmail.Text, mobilePhoneNumber );
                List<Person> personMatchResults = personService.FindPersons( personQuery, false, false ).ToList();

                if ( personMatchResults.Count == 1 )
                {
                    person = personMatchResults.Single();
                }
            }

            if ( person == null && tbFirstName.Text.IsNotNullOrWhiteSpace() && tbLastName.Text.IsNotNullOrWhiteSpace() )
            {
                // single match wasnt found and we have at least first and last name, create new person
                person = CreateAndSaveNewPerson();
            }

            if ( person != null )
            {
                // person succesfully found or created
                RockPage.LinkPersonAliasToDevice( person.PrimaryAlias.Id, hfMacAddress.Value );
                return person.PrimaryAlias.Id;
            }

            // Unable to find an existing user and we don't have the minimium info to create one.
            // We'll let Rock.Page and the cookie created in OnLoad() link the device to a user when they are logged in.
            // This will not work if this page is loaded into a Captive Network Assistant page as the cookie will not persist.
            // CCV requires first name and last name, so this should never happen...but leaving just in case...
            return null;
        }

        protected Person CreateAndSaveNewPerson()
        {
            var rockContext = new RockContext();

            int mobilePhoneTypeId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE ).Id;

            var recordTypeValue = DefinedValueCache.Read( GetAttributeValue( "NewPersonRecordType" ).AsGuid() ) ?? DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() );
            var recordStatusValue = DefinedValueCache.Read( GetAttributeValue( "NewPersonRecordStatus" ).AsGuid() ) ?? DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() );
            var connectionStatusValue = DefinedValueCache.Read( GetAttributeValue( "NewPersonConnectionStatus" ).AsGuid() ) ?? DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_VISITOR.AsGuid() );

            var person = new Person
            {
                FirstName = tbFirstName.Text,
                LastName = tbLastName.Text,
                RecordTypeValueId = recordTypeValue != null ? recordTypeValue.Id : ( int? ) null,
                RecordStatusValueId = recordStatusValue != null ? recordStatusValue.Id : ( int? ) null,
                ConnectionStatusValueId = connectionStatusValue != null ? connectionStatusValue.Id : ( int? ) null
            };

            if ( tbEmail.Text.IsNotNullOrWhiteSpace() )
            {
                person.Email = tbEmail.Text;
            }
            
            if ( tbMobilePhone.Text.RemoveAllNonAlphaNumericCharacters().IsNotNullOrWhiteSpace() )
            {
                person.PhoneNumbers = new List<PhoneNumber>() { new PhoneNumber { IsSystem = false, Number = tbMobilePhone.Text.RemoveAllNonAlphaNumericCharacters(), NumberTypeValueId = mobilePhoneTypeId } };
            }

            PersonService.SaveNewPerson( person, rockContext );

            string campusHistoryText = "";

            if ( hfCampusId.Value.IsNotNullOrWhitespace() )
            {
                // we have a location campus id, get the campus and add to the family
                var campus = new CampusService( rockContext ).Get( hfCampusId.Value.AsInteger() );

                if ( campus != null )
                {
                    var family = person.GetFamily( rockContext );

                    family.CampusId = campus.Id;

                    // add campus to history message
                    campusHistoryText = string.Format( " at {0} campus", campus.Name );
                }
            }

            // add message in person history that this came from the captive portal
            var changes = new List<string>
            {
                String.Format( "Created by Wifi Captive Portal{0}", campusHistoryText)
            };

            HistoryService.AddChanges( rockContext, typeof( Person ), Rock.SystemGuid.Category.HISTORY_PERSON_DEMOGRAPHIC_CHANGES.AsGuid(), person.Id, changes );
            rockContext.SaveChanges();

            return person;
        }

        /// <summary>
        /// Redirects the specified person to the release link URL
        /// </summary>
        /// <param name="primaryAliasId">if null then the id parameter is not created for the returned URL</param>
        /// <returns></returns>
        protected string CreateRedirectUrl( int? primaryAliasId )
        {
            string s = string.Empty;

            if ( primaryAliasId != null )
            {
                s = string.Format( "{0}?id={1}&{2}", GetAttributeValue( "ReleaseUrl" ), primaryAliasId, Request.QueryString );
                return s;
            }

            s = string.Format( "{0}?{1}", GetAttributeValue( "ReleaseUrl" ), Request.QueryString );
            return s;
        }

        /// <summary>
        /// Updates the CurrentPerson email and mobile phone number with the entered values if they are different.
        /// Does nothing if there is no CurrentPerson
        /// </summary>
        private void UpdatePersonInfo()
        {
            if ( CurrentPerson == null )
            {
                return;
            }

            using ( RockContext rockContext = new RockContext() )
            {
                Person person = new PersonService( rockContext ).Get( ( int ) CurrentPersonId );
                person.Email = tbEmail.Text;

                int mobilePhoneTypeId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE ).Id;
                if ( !person.PhoneNumbers.Where( n => n.NumberTypeValueId == mobilePhoneTypeId ).Any() &&
                    tbMobilePhone.Text.RemoveAllNonAlphaNumericCharacters().IsNotNullOrWhiteSpace() )
                {
                    person.PhoneNumbers.Add( new PhoneNumber { IsSystem = false, Number = tbMobilePhone.Text.RemoveAllNonAlphaNumericCharacters(), NumberTypeValueId = mobilePhoneTypeId } );
                }
                else
                {
                    PhoneNumber phoneNumber = person.PhoneNumbers.Where( p => p.NumberTypeValueId == mobilePhoneTypeId ).FirstOrDefault();
                    if ( phoneNumber != null )
                    {
                        phoneNumber.Number = tbMobilePhone.Text.RemoveAllNonAlphaNumericCharacters();
                    }
                }

                rockContext.SaveChanges();
            }
        }
    }
}