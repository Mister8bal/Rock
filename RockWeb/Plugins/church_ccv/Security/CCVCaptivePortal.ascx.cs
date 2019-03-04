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
    [DisplayName( "CCV Captive Portal" )]
    [Category( "CCV > Security" )]
    [Description( "Controls access to Wi-Fi." )]

    #region Block Settings
    [TextField(
        name: "MAC Address Paramameter",
        description: "The query string parameter used for the MAC Address",
        defaultValue: "client_mac",
        order: 0,
        key: "MacAddressParam" )]
    [TextField(
        name: "Release Link",
        description: "The full URL to redirect users to after registration.",
        order: 1,
        key: "ReleaseLink" )]
    [BooleanField(
        name: "Show Name",
        description: "Show or hide the Name fields. If it is visible then it will be required.",
        defaultValue: true,
        order: 2,
        key: "ShowName",
        IsRequired = true )]
    [BooleanField(
        name: "Show Mobile Phone",
        description: "Show or hide the Mobile Phone Number field. If it is visible then it will be required.",
        defaultValue: true,
        order: 3,
        key: "ShowMobilePhone",
        IsRequired = true )]
    [BooleanField(
        name: "Show Email",
        description: "Show or hide the Email field. If it is visible then it will be required.",
        defaultValue: true,
        order: 4,
        key: "ShowEmail",
        IsRequired = true )]
    [TextField(
        name: "Title Text",
        description: "Title Text to display",
        defaultValue: "Thank you for joining us.",
        order: 5,
        key: "TitleText" )]
    [TextField(
        name: "Button Text",
        description: "Text to display on the button",
        defaultValue: "Connect",
        order: 7,
        key: "ButtonText" )]
    [BooleanField(
        name: "Show Legal Note",
        description: "Show or hide the Terms and Conditions. This should be always be visible unless users are being automatically connected without any agreement needed.",
        defaultValue: true,
        order: 8,
        key: "ShowLegalNote",
        IsRequired = true )]
    [DefinedValueField(
        definedTypeGuid: Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON,
        name: "New Person Record Type",
        description: "The person type to assign to new persons created by Captive Portal.",
        defaultValue: Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON,
        order: 9,
        key: "NewPersonRecordType" )]
    [DefinedValueField(
        definedTypeGuid: Rock.SystemGuid.DefinedType.PERSON_RECORD_STATUS,
        name: "New Person Record Status",
        description: "The record status to assign to new persons created by Captive Portal.",
        defaultValue: Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE,
        order: 10,
        key: "NewPersonRecordStatus" )]
    [DefinedValueField(
        definedTypeGuid: Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS,
        name: "New Person Connection Status",
        description: "The connection status to assign to new persons created by Captive Portal",
        defaultValue: Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_VISITOR,
        order: 11,
        key: "NewPersonConnectionStatus" )]
    [CodeEditorField(
        name: "Legal Note",
        description: "A legal note outlining the Terms and Conditions for using Wi-Fi.",
        mode: CodeEditorMode.Html,
        height: 400,
        required: false,
        defaultValue: DEFAULT_LEGAL_NOTE,
        order: 12,
        key: "LegalNote" )]

    #endregion Block Settings
    public partial class CCVCaptivePortal : RockBlock
    {
        #region Block Setting Strings
        protected const string DEFAULT_LEGAL_NOTE = @"
<div>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Helvetica, Arial, sans-serif, ""Apple Color Emoji"", ""Segoe UI Emoji"", ""Segoe UI Symbol""; }
    </style>
    <h3>Terms & Conditions</h3>
    <p>By using our internet service, you hereby expressly acknowledge and agree that there are significant security, privacy and confidentiality risks inherent in accessing or transmitting information through the internet, whether the connection is facilitated through wired or wireless technology. Security issues include, without limitation, interception of transmissions, loss of data, and the introduction or viruses and other programs that can corrupt or damage your computer.</p>
    <p>Accordingly, you agree that the owner and/or provider of this network is NOT liable for any interception or transmissions, computer worms or viruses, loss of data, file corruption, hacking or damage to your computer or other devices that result from the transmission or download of information or materials through the internet service provided.</p>
    <p>Use of the wireless network is subject to the general restrictions outlined below. If abnormal, illegal, or unauthorized behavior is detected, including heavy consumption of bandwidth, the network provider reserves the right to permanently disconnect the offending device from the wireless network.</p>
    <h5>Examples of Illegal Uses</h5>
    <p>The following are representative examples only and do not comprise a comprehensive list of illegal uses:</p>
    <ol>
        <li>Spamming and invasion of privacy - Sending of unsolicited bulk and/or commercial messages over the Internet using the Service or using the Service for activities that invade another's privacy.</li>
        <li>Intellectual property right violations - Engaging in any activity that infringes or misappropriates the intellectual property rights of others, including patents, copyrights, trademarks, service marks, trade secrets, or any other proprietary right of any third party.</li>
        <li>Accessing illegally or without authorization computers, accounts, equipment or networks belonging to another party, or attempting to penetrate/circumvent security measures of another system. This includes any activity that may be used as a precursor to an attempted system penetration, including, but not limited to, port scans, stealth scans, or other information gathering activity.</li>
        <li>The transfer of technology, software, or other materials in violation of applicable export laws and regulations.</li>
        <li>Export Control Violations</li>
        <li>Using the Service in violation of applicable law and regulation, including, but not limited to, advertising, transmitting, or otherwise making available ponzi schemes, pyramid schemes, fraudulently charging credit cards, pirating software, or making fraudulent offers to sell or buy products, items, or services.</li>
        <li>Uttering threats.</li>
        <li>Distribution of pornographic materials to minors.</li>
        <li>Child pornography.</li>
    </ol>
    <h5>Examples of Unacceptable Uses</h5>
    <p>The following are representative examples only and do not comprise a comprehensive list of unacceptable uses:</p>
    <ol>
        <li>Accessing pornography content</li>
        <li>High bandwidth operations, such as large file transfers and media sharing with peer-to-peer programs (i.e.torrents)</li>
        <li>Obscene or indecent speech or materials</li>
        <li>Defamatory or abusive language</li>
        <li>Using the Service to transmit, post, upload, or otherwise making available defamatory, harassing, abusive, or threatening material or language that encourages bodily harm, destruction of property or harasses another.</li>
        <li>Forging or misrepresenting message headers, whether in whole or in part, to mask the originator of the message.</li>
        <li>Facilitating a Violation of these Terms of Use</li>
        <li>Hacking</li>
        <li>Distribution of Internet viruses, Trojan horses, or other destructive activities</li>
        <li>Distributing information regarding the creation of and sending Internet viruses, worms, Trojan horses, pinging, flooding, mail-bombing, or denial of service attacks. Also, activities that disrupt the use of or interfere with the ability of others to effectively use the node or any connected network, system, service, or equipment.</li>
        <li>Advertising, transmitting, or otherwise making available any software product, product, or service that is designed to violate these Terms of Use, which includes the facilitation of the means to spam, initiation of pinging, flooding, mail-bombing, denial of service attacks, and piracy of software.</li>
        <li>The sale, transfer, or rental of the Service to customers, clients or other third parties, either directly or as part of a service or product created for resale.</li>
        <li>Seeking information on passwords or data belonging to another user.</li>
        <li>Making unauthorized copies of proprietary software, or offering unauthorized copies of proprietary software to others.</li>
        <li>Intercepting or examining the content of messages, files or communications in transit on a data network.</li>
    </ol>
</div>";
        #endregion

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
                    ShowControls( false );
                    return;
                }

                string releaseLink = GetAttributeValue( "ReleaseLink" );
                if ( string.IsNullOrWhiteSpace( releaseLink ) || !releaseLink.IsValidUrl() )
                {
                    nbAlert.Text = "Missing or invalid Release Link";
                    nbAlert.Visible = true;
                    ShowControls( false );
                    return;
                }

                // Save the supplied MAC address to the page removing any non-Alphanumeric characters
                macAddress = macAddress.RemoveAllNonAlphaNumericCharacters();
                hfMacAddress.Value = macAddress;

                // create or get device
                PersonalDeviceService personalDeviceService = new PersonalDeviceService( new RockContext() );
                PersonalDevice personalDevice = null;

                if ( DoesPersonalDeviceExist( macAddress ) )
                {
                    personalDevice = VerifyDeviceInfo( macAddress );
                }
                else
                {
                    personalDevice = CreateDevice( macAddress );
                }

                // We are going to create this everytime they hit the captive portal page. Otherwise if the device is saved but not linked to an actual user (not the fake one created here),
                // and then deleted by the user/browser/software, then they'll never get the cookie again and won't automatically get linked by RockPage.
                CreateDeviceCookie( macAddress );

                // See if user is logged in and link the alias to the device.
                if ( CurrentPerson != null )
                {
                    Prefill( CurrentPerson );
                    RockPage.LinkPersonAliasToDevice( ( int ) CurrentPersonAliasId, macAddress );
                }

                // Set the title text
                if ( GetAttributeValue( "TitleText" ).IsNotNullOrWhiteSpace() )
                {
                    lblTitleText.Text = GetAttributeValue( "TitleText" );
                }

                // Direct connect if no controls are visible
                if ( !ShowControls() )
                {
                    // Nothing to show means nothing to enter. Redirect user back to FP with the primary alias ID and query string
                    if ( IsUserAuthorized( Rock.Security.Authorization.ADMINISTRATE ) )
                    {
                        nbAlert.Text = string.Format( "If you did not have Administrative permissions on this block you would have been redirected to: <a href='{0}'>{0}</a>.", CreateRedirectUrl( null ) );
                    }
                    else
                    {
                        Response.Redirect( CreateRedirectUrl( null ) );
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Doeses a personal device exist for the provided MAC address
        /// </summary>
        /// <param name="macAddress">The mac address.</param>
        /// <returns></returns>
        private bool DoesPersonalDeviceExist( string macAddress )
        {
            PersonalDeviceService personalDeviceService = new PersonalDeviceService( new RockContext() );
            return personalDeviceService.GetByMACAddress( macAddress ) == null ? false : true;
        }

        /// <summary>
        /// Creates the device if new.
        /// </summary>
        /// <returns>Returns true if the device was created, false it already existed</returns>
        private PersonalDevice CreateDevice( string macAddress )
        {
            UAParser.ClientInfo client = UAParser.Parser.GetDefault().Parse( Request.UserAgent );

            RockContext rockContext = new RockContext();
            PersonalDeviceService personalDeviceService = new PersonalDeviceService( rockContext );

            PersonalDevice personalDevice = new PersonalDevice();
            personalDevice.MACAddress = macAddress;

            personalDevice.PersonalDeviceTypeValueId = GetDeviceTypeValueId();
            personalDevice.PlatformValueId = GetDevicePlatformValueId( client );
            personalDevice.DeviceVersion = GetDeviceOsVersion( client );

            personalDeviceService.Add( personalDevice );
            rockContext.SaveChanges();

            return personalDevice;
        }

        /// <summary>
        /// Gets the current device platform info and updates the obj if needed.
        /// </summary>
        /// <param name="personalDevice">The personal device.</param>
        private PersonalDevice VerifyDeviceInfo( string macAddress )
        {
            UAParser.ClientInfo client = UAParser.Parser.GetDefault().Parse( Request.UserAgent );

            RockContext rockContext = new RockContext();
            PersonalDeviceService personalDeviceService = new PersonalDeviceService( rockContext );

            PersonalDevice personalDevice = personalDeviceService.GetByMACAddress( macAddress );
            personalDevice.PersonalDeviceTypeValueId = GetDeviceTypeValueId();
            personalDevice.PlatformValueId = GetDevicePlatformValueId( client );
            personalDevice.DeviceVersion = GetDeviceOsVersion( client );

            rockContext.SaveChanges();

            return personalDevice;
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
        /// Prefills the visible fields with info from the specified rock person
        /// if there is a logged in user than the name fields will be disabled.
        /// </summary>
        /// <param name="rockUserId">The rock user identifier.</param>
        protected void Prefill( Person person )
        {
            if ( person == null )
            {
                return;
            }

            if ( tbFirstName.Visible == true )
            {
                tbFirstName.Text = person.FirstName;
                tbFirstName.Enabled = CurrentPerson == null;

                tbLastName.Text = person.LastName;
                tbLastName.Enabled = CurrentPerson == null;
            }

            if ( tbMobilePhone.Visible )
            {
                PhoneNumberService phoneNumberService = new PhoneNumberService( new RockContext() );
                PhoneNumber phoneNumber = phoneNumberService.GetNumberByPersonIdAndType( person.Id, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE );
                tbMobilePhone.Text = phoneNumber == null ? string.Empty : phoneNumber.Number;
            }

            if ( tbEmail.Visible == true )
            {
                tbEmail.Text = person.Email;
            }
        }

        /// <summary>
        /// Handles the Click event of the btnConnect control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnConnect_Click( object sender, EventArgs e )
        {
            // We know there is a device with the stored MAC
            // If we have an alias ID then we have all data needed and can redirect the user to frontporch
            // also if hfPersonAliasId has a value at this time it has already been linked to the device.
            if ( CurrentPerson != null )
            {
                UpdatePersonInfo();
                Response.Redirect( CreateRedirectUrl( CurrentPersonAliasId ) );
                return;
            }

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
            // At this point the user is not logged in and not found by looking up the device
            // So lets try to find the user using entered info and then link them to the device.
            PersonService personService = new PersonService( new RockContext() );
            int mobilePhoneTypeId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE ).Id;
            Person person = null;
            string mobilePhoneNumber = string.Empty;

            // Looking for a 100% match
            if ( tbFirstName.Visible && tbLastName.Visible && tbEmail.Visible && tbMobilePhone.Visible )
            {
                mobilePhoneNumber = tbMobilePhone.Text.RemoveAllNonAlphaNumericCharacters();

                var personQuery = new PersonService.PersonMatchQuery( tbFirstName.Text, tbLastName.Text, tbEmail.Text, mobilePhoneNumber );
                person = personService.FindPerson( personQuery, true );

                if ( person.IsNotNull() )
                {
                    RockPage.LinkPersonAliasToDevice( person.PrimaryAlias.Id, hfMacAddress.Value );
                    return person.PrimaryAliasId;
                }
                else
                {
                    // If no known person record then create one since we have the minimum info required
                    person = CreateAndSaveNewPerson();

                    // Link new device to person alias
                    RockPage.LinkPersonAliasToDevice( person.PrimaryAlias.Id, hfMacAddress.Value );
                    return person.PrimaryAlias.Id;
                }
            }

            // Look for minimum info
            if ( tbFirstName.Visible && tbLastName.Visible && ( tbMobilePhone.Visible || tbEmail.Visible ) )
            {
                // If no known person record then create one since we have the minimum info required
                person = CreateAndSaveNewPerson();

                // Link new device to person alias
                RockPage.LinkPersonAliasToDevice( person.PrimaryAlias.Id, hfMacAddress.Value );
                return person.PrimaryAlias.Id;
            }

            // Just match off phone number if no other fields are showing.
            if ( tbMobilePhone.Visible && !tbFirstName.Visible && !tbLastName.Visible && !tbEmail.Visible )
            {
                mobilePhoneNumber = tbMobilePhone.Text.RemoveAllNonAlphaNumericCharacters();
                person = personService.Queryable().Where( p => p.PhoneNumbers.Where( n => n.NumberTypeValueId == mobilePhoneTypeId ).FirstOrDefault().Number == mobilePhoneNumber ).FirstOrDefault();
                if ( person != null )
                {
                    RockPage.LinkPersonAliasToDevice( person.PrimaryAlias.Id, hfMacAddress.Value );
                    return person.PrimaryAliasId;
                }
            }

            // Unable to find an existing user and we don't have the minimium info to create one.
            // We'll let Rock.Page and the cookie created in OnLoad() link the device to a user when they are logged in.
            // This will not work if this page is loaded into a Captive Network Assistant page as the cookie will not persist.
            return null;
        }

        protected Person CreateAndSaveNewPerson()
        {
            int mobilePhoneTypeId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE ).Id;

            var recordTypeValue = DefinedValueCache.Read( GetAttributeValue( "NewPersonRecordType" ).AsGuid() ) ?? DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() );
            var recordStatusValue = DefinedValueCache.Read( GetAttributeValue( "NewPersonRecordStatus" ).AsGuid() ) ?? DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() );
            var connectionStatusValue = DefinedValueCache.Read( GetAttributeValue( "NewPersonConnectionStatus" ).AsGuid() ) ?? DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_VISITOR.AsGuid() );

            var person = new Person
            {
                FirstName = tbFirstName.Text,
                LastName = tbLastName.Text,
                Email = tbEmail.Text,
                RecordTypeValueId = recordTypeValue != null ? recordTypeValue.Id : ( int? ) null,
                RecordStatusValueId = recordStatusValue != null ? recordStatusValue.Id : ( int? ) null,
                ConnectionStatusValueId = connectionStatusValue != null ? connectionStatusValue.Id : ( int? ) null
            };

            if ( tbMobilePhone.Text.RemoveAllNonAlphaNumericCharacters().IsNotNullOrWhiteSpace() )
            {
                person.PhoneNumbers = new List<PhoneNumber>() { new PhoneNumber { IsSystem = false, Number = tbMobilePhone.Text.RemoveAllNonAlphaNumericCharacters(), NumberTypeValueId = mobilePhoneTypeId } };
            }

            PersonService.SaveNewPerson( person, new RockContext() );
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
                s = string.Format( "{0}?id={1}&{2}", GetAttributeValue( "ReleaseLink" ), primaryAliasId, Request.QueryString );
                return s;
            }

            s = string.Format( "{0}?{1}", GetAttributeValue( "ReleaseLink" ), Request.QueryString );
            return s;
        }

        /// <summary>
        /// Shows the controls according to the attribute values. If they are visible then they are also required.
        /// </summary>
        /// <returns>If any control is visible then true, else false.</returns>
        protected bool ShowControls( bool isEnabled = true )
        {
            tbFirstName.Visible = GetAttributeValue( "ShowName" ).AsBoolean();
            tbFirstName.Required = GetAttributeValue( "ShowName" ).AsBoolean();
            tbFirstName.Enabled = isEnabled;

            tbLastName.Visible = GetAttributeValue( "ShowName" ).AsBoolean();
            tbLastName.Required = GetAttributeValue( "ShowName" ).AsBoolean();
            tbLastName.Enabled = isEnabled;

            tbMobilePhone.Visible = GetAttributeValue( "ShowMobilePhone" ).AsBoolean();
            tbMobilePhone.Required = GetAttributeValue( "ShowMobilePhone" ).AsBoolean();
            tbMobilePhone.Enabled = isEnabled;

            tbEmail.Visible = GetAttributeValue( "ShowEmail" ).AsBoolean();
            tbEmail.Required = GetAttributeValue( "ShowEmail" ).AsBoolean();
            tbEmail.Enabled = isEnabled;

            btnConnect.Text = isEnabled ? GetAttributeValue( "ButtonText" ) : "Connect";
            btnConnect.Enabled = isEnabled;

            if ( litLegalNotice.Visible = GetAttributeValue( "ShowLegalNote" ).AsBoolean() )
            {
                litLegalNotice.Text = GetAttributeValue( "LegalNote" ).ResolveMergeFields( Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage ) );
            }

            if ( tbFirstName.Visible || tbLastName.Visible || tbMobilePhone.Visible || tbEmail.Visible || litLegalNotice.Visible )
            {
                return true;
            }

            return false;
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