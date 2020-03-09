using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using church.ccv.CCVCore.HubSpot.Model;
using Newtonsoft.Json.Linq;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using static church.ccv.CCVCore.HubSpot.Util.HubSpotConstants;

namespace church.ccv.CCVCore.HubSpot.Util
{
    public class HubSpotApi
    {
        /// <summary>
        /// Request contact info from HubSpot
        /// </summary>
        /// <param name="hubSpotObjectId"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> GetContactFromHubSpot( int hubSpotObjectId )
        {
            return await GetContactFromHubSpot( hubSpotObjectId, true, false, new List<string>() );
        }

        /// <summary>
        /// Request contact info from HubSpot
        /// </summary>
        /// <param name="hubSpotObjectId"></param>
        /// <param name="includeStandardProperties"></param>
        /// <param name="includeAddress"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> GetContactFromHubSpot( int hubSpotObjectId, bool includeStandardProperties, bool includeAddress )
        {
            return await GetContactFromHubSpot( hubSpotObjectId, includeStandardProperties, includeAddress, new List<string>() );
        }

        /// <summary>
        /// Request contact info from HubSpot
        /// </summary>
        /// <param name="hubSpotObjectId"></param>
        /// <param name="includeStandardProperties"></param>
        /// <param name="includeAddress"></param>
        /// <param name="contactProperties"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> GetContactFromHubSpot( int hubSpotObjectId, bool includeStandardProperties, bool includeAddress, List<string> contactProperties )
        {
            HttpResponseMessage hubSpotResponse = new HttpResponseMessage();

            // hubspot api configuration
            string hubSpot_APIUrl = GlobalAttributesCache.Value( Api.Url );
            string hubSpot_APIKey = GlobalAttributesCache.Value( Api.Key );
            if ( hubSpot_APIUrl.IsNullOrWhiteSpace() || hubSpot_APIKey.IsNullOrWhiteSpace() )
            {
                // missing required configuration
                throw new Exception( "Missing HubSpot API Url or Key" );
            }

            // build api url
            StringBuilder requestUrl = new StringBuilder( hubSpot_APIUrl );
            requestUrl.Append( "/contacts/v1/contact/vid/" + hubSpotObjectId + "/profile" );
            requestUrl.Append( "?hapikey=" + hubSpot_APIKey );
            requestUrl.Append( "&propertyMode=value_only" );
            requestUrl.Append( "&formSubmissionMode=none" );

            // add standard properties
            if ( includeStandardProperties == true )
            {
                requestUrl.Append( "&property=" + ContactPropertyKey.FirstName );
                requestUrl.Append( "&property=" + ContactPropertyKey.LastName );
                requestUrl.Append( "&property=" + ContactPropertyKey.Email );
                requestUrl.Append( "&property=" + ContactPropertyKey.DateOfBirth );
                requestUrl.Append( "&property=" + ContactPropertyKey.RockId );
                requestUrl.Append( "&property=" + ContactPropertyKey.Phone );
                requestUrl.Append( "&property=" + ContactPropertyKey.MobilePhone );
            }

            if ( includeAddress == true )
            {
                requestUrl.Append( "&property=" + ContactPropertyKey.Address );
                requestUrl.Append( "&property=" + ContactPropertyKey.City );
                requestUrl.Append( "&property=" + ContactPropertyKey.State );
                requestUrl.Append( "&property=" + ContactPropertyKey.Zip );
                requestUrl.Append( "&property=" + ContactPropertyKey.Country );
            }

            // add properties to api url
            foreach ( string contactProperty in contactProperties )
            {
                requestUrl.Append( "&property=" );
                requestUrl.Append( contactProperty );
            }

            // make the api request
            try
            {
                HttpClient client = new HttpClient();
                hubSpotResponse = await client.GetAsync( requestUrl.ToString() );
            }
            catch ( Exception e )
            {
                Debug.WriteLine( e.Message );
            }

            return hubSpotResponse;
        }

        /// <summary>
        /// Request contact address from HubSpot
        /// </summary>
        /// <param name="rockContext"></param>
        /// <param name="hubSpotObjectId"></param>
        /// <returns></returns>
        public static async Task<Location> GetContactAddressFromHubSpot( RockContext rockContext, int hubSpotObjectId )
        {

            HttpResponseMessage hubSpotApiResponse = await GetContactFromHubSpot( hubSpotObjectId, false, true, new List<string>() );
            string responseString = await hubSpotApiResponse.Content.ReadAsStringAsync();
            if ( hubSpotApiResponse.StatusCode != HttpStatusCode.OK || responseString.IsNullOrWhiteSpace() )
            {
                // fail - api call failed or response empty
                throw new Exception( "RequestContact: HubSpot API Error" );
            }

            // parse the response string into JObject
            JObject response = JObject.Parse( responseString );
            JObject responseProperties = ( JObject ) response[WebHookEventKey.Properties];

            string address = ParseResponsePropertyWithValue( responseProperties, ContactPropertyKey.Address );
            string city = ParseResponsePropertyWithValue( responseProperties, ContactPropertyKey.City );
            string state = ParseResponsePropertyWithValue( responseProperties, ContactPropertyKey.State );
            string zip = ParseResponsePropertyWithValue( responseProperties, ContactPropertyKey.Zip );
            string country = ParseResponsePropertyWithValue( responseProperties, ContactPropertyKey.Country );

            if ( address.IsNullOrWhiteSpace() || city.IsNullOrWhiteSpace() || state.IsNullOrWhiteSpace() || zip.IsNullOrWhiteSpace() || country.IsNullOrWhiteSpace() )
            {
                // missing required info to get new location
                return null;
            }

            return new LocationService( rockContext )
                .Get( address, "", city, state, zip, country, true );
        }

        /// <summary>
        /// Update contact property in HubSpot
        /// </summary>
        /// <param name="hubSpotObjectId"></param>
        /// <param name="propertyKey"></param>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        public static async Task<ApiResult> UpdateContactInHubSpot( int hubSpotObjectId, int personId, string propertyKey, string propertyValue )
        {
            Dictionary<string, string> propertiesToUpdate = new Dictionary<string, string>
            {
                { propertyKey, propertyValue }
            };
            return await UpdateContactInHubSpot( hubSpotObjectId, personId, propertiesToUpdate );
        }

        /// <summary>
        /// Update contact properties in HubSpot
        /// </summary>
        /// <param name="hubSpotObjectId"></param>
        /// <param name="propertiesToUpdate"></param>
        /// <returns></returns>
        public static async Task<ApiResult> UpdateContactInHubSpot( int hubSpotObjectId, int personId, Dictionary<string, string> propertiesToUpdate )
        {
            // default return value
            ApiResult apiResult = ApiResult.Failed;

            // hubspot api configuration
            string hubSpot_APIUrl = GlobalAttributesCache.Value( Api.Url );
            string hubSpot_APIKey = GlobalAttributesCache.Value( Api.Key );
            if ( hubSpot_APIUrl.IsNullOrWhiteSpace() || hubSpot_APIKey.IsNullOrWhiteSpace() || propertiesToUpdate.Count == 0 )
            {
                // missing required configuration or no properties passed to update
                throw new Exception( "Missing HubSpot API Url or Key" );
            }

            // build api url
            StringBuilder requestUrl = new StringBuilder( hubSpot_APIUrl );
            requestUrl.Append( "/contacts/v1/contact/vid/" + hubSpotObjectId + "/profile" );
            requestUrl.Append( "?hapikey=" + hubSpot_APIKey );

            // build request body
            StringBuilder requestBody = new StringBuilder( "{ \"properties\": [" );
            int numPropertiesAdded = 0;
            foreach ( var property in propertiesToUpdate )
            {
                numPropertiesAdded++;

                requestBody.Append( string.Format( "{{ \"property\": \"{0}\",\"value\": \"{1}\" }}", property.Key, property.Value ) );

                if ( numPropertiesAdded != propertiesToUpdate.Count )
                {
                    requestBody.Append( "," );
                }
            }
            requestBody.Append( "] }" );

            // try the update
            bool tryUpdate = true;
            int numberOfTries = 0;
            HttpClient client = new HttpClient();
            while ( tryUpdate == true )
            {
                HttpResponseMessage hubSpotResponse = new HttpResponseMessage();
                try
                {
                    hubSpotResponse = await client.PostAsync( requestUrl.ToString(), new StringContent( requestBody.ToString(), Encoding.UTF8, "application/json" ) );
                }
                catch ( Exception e )
                {
                    Debug.WriteLine( e.Message );
                }

                // 204 no content is the success status from HubSpot
                if ( hubSpotResponse.StatusCode == HttpStatusCode.NoContent )
                {
                    // add messages in person history
                    HubSpotHistoryService.AddHubSpotHistory( SyncDirection.ToHubSpot, personId, propertiesToUpdate );
                    apiResult = ApiResult.Success;
                    tryUpdate = false;
                }

                if ( numberOfTries > 2 )
                {
                    tryUpdate = false;
                }

                numberOfTries++;
            }

            return apiResult;
        }       

        /// <summary>
        /// Sync contact info to HubSpot
        /// </summary>
        /// <param name="hubSpotContact"></param>
        /// <param name="fieldsToSync"></param>
        /// <returns></returns>
        public static async Task<ApiResult> SyncContactToHubSpot( HubSpotContact hubSpotContact, List<string> fieldsToSync )
        {
            // setup propertiesToUpdate
            Person person = hubSpotContact.PersonAlias.Person;
            Dictionary<string, string> propertiesToUpdate = new Dictionary<string, string>();

            // add fields to propertiesToUpdate
            foreach ( var field in fieldsToSync )
            {
                // Important: HubSpot does not support subscribing contacts to email lists
                // or opting in to email through their API. They do this to keep their system
                // from being abused.  Due to this, we do not support Syncing email preferences
                // from Rock to HubSpot.
                string newKey = "";
                string newValue = "";
                switch ( field )
                {
                    // hubspot does not have a seperate property for nick name
                    case RockHistoryFieldName.FirstName:
                    case RockHistoryFieldName.NickName:
                        // ensure firstname wasnt already added
                        if ( !propertiesToUpdate.ContainsKey( ContactPropertyKey.FirstName ) )
                        {
                            newKey = ContactPropertyKey.FirstName;
                            // since this is marketing, always use NickName value from rock
                            newValue = person.NickName;
                        }
                        break;
                    case RockHistoryFieldName.LastName:
                        newKey = ContactPropertyKey.LastName;
                        newValue = person.LastName;
                        break;
                    case RockHistoryFieldName.Email:
                        newKey = ContactPropertyKey.Email;
                        newValue = person.Email;
                        break;
                    case RockHistoryFieldName.Phone:
                        newKey = ContactPropertyKey.Phone;
                        newValue = person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() ).ToString().RemoveAllNonAlphaNumericCharacters();
                        break;
                    case RockHistoryFieldName.MobilePhone:
                        newKey = ContactPropertyKey.MobilePhone;
                        newValue = person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ).ToString().RemoveAllNonAlphaNumericCharacters();
                        break;
                    case RockHistoryFieldName.BirthDate:
                        if ( person.BirthDate != null )
                        {
                            newKey = ContactPropertyKey.DateOfBirth;
                            newValue = person.BirthDate?.ToString( "MM/dd/yyyy" );
                        }
                        break;
                    case RockHistoryFieldName.Age:
                        if ( person.Age != null )
                        {
                            newKey = ContactPropertyKey.Age;
                            newValue = person.Age.ToString();
                        }
                        break;
                    case RockHistoryFieldName.ConnectionStatus:
                        newValue = ConvertConnectionStatusToHubSpotProperty( person );
                        if ( newValue.IsNotNullOrWhiteSpace() )
                        {
                            newKey = ContactPropertyKey.ConnectionStatus;
                        }
                        break;
                    case RockHistoryFieldName.RecordSatus:
                        newKey = ContactPropertyKey.MemberStatus;
                        newValue = ConvertRecordStatustoHubSpotProperty( person );
                        break;
                    case RockHistoryFieldName.Street:
                    case RockHistoryFieldName.City:
                    case RockHistoryFieldName.State:
                    case RockHistoryFieldName.ZipCode:
                    case RockHistoryFieldName.Country:
                        // address syncing is not supported at this time
                        // it will be implemented after we upgrade to a version of Rock
                        // that supports group history
                    default:
                        break;
                }

                // add the item if its not blank and if its not already in update object
                if ( newKey.IsNotNullOrWhiteSpace() && !propertiesToUpdate.ContainsKey( newKey ) )
                {
                    propertiesToUpdate.Add( newKey, newValue );
                }
            }

            if ( propertiesToUpdate.Count == 0 )
            {
                // nothing to update
                return ApiResult.NothingToSync;
            }

            // add rock id (person alias id) to update to keep it in sync
            propertiesToUpdate.Add( ContactPropertyKey.RockId, hubSpotContact.PersonAliasId.ToString() );

            return await UpdateContactInHubSpot( hubSpotContact.HubSpotObjectId, hubSpotContact.PersonAlias.PersonId, propertiesToUpdate );
        }

        #region Helper Methods

        /// <summary>
        /// Return the equivalant HubSpot property for Member Status
        /// </summary>
        /// <param name="person"></param>
        /// <returns></returns>
        private static string ConvertRecordStatustoHubSpotProperty( Person person )
        {
            int recordStatusActiveId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() ).Id;
            int recordStatusPendingId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE.AsGuid() ).Id;
            int recordStatusInactiveId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() ).Id;

            if ( person.RecordStatusValueId == recordStatusActiveId || person.RecordStatusValueId == recordStatusPendingId )
            {
                return ContactPropertyValue.MemberStatus_Active;
            }
            else if ( person.RecordStatusValueId == recordStatusInactiveId )
            {
                return ContactPropertyValue.MemberStatus_Inactive;
            }

            return string.Empty;            
        }

        /// <summary>
        /// Return the equivalant HubSpot property connections status. Defaults to Visior
        /// </summary>
        /// <param name="person"></param>
        /// <returns></returns>
        private static string ConvertConnectionStatusToHubSpotProperty( Person person )
        {
            switch ( person.ConnectionStatusValue.Value )
            {
                case "Member":
                    return ContactPropertyValue.ConnectionStatus_Member;
                case "Attendee":
                    return ContactPropertyValue.ConnectionStatus_Attendee;
                case "Visitor":
                    return ContactPropertyValue.ConnectionStatus_Visitor;
                case "Participant":
                    return ContactPropertyValue.ConnectionStatus_Participant;
                case "STARS Participant":
                    return ContactPropertyValue.ConnectionStatus_StarsParticipant;
                case "Web Prospect":
                    return ContactPropertyValue.ConnectionStatus_WebProspect;
                case "Visitor Prospect":
                    return ContactPropertyValue.ConnectionStatus_VisitorProspect;
                case "Former Member":
                    return ContactPropertyValue.ConnectionStatus_FormerMember;
                case "Missionary":
                    return ContactPropertyValue.ConnectionStatus_Missionary;
                case "Corporate":
                    return ContactPropertyValue.ConnectionStatus_Corporate;
                default:
                    return ContactPropertyValue.ConnectionStatus_Visitor;
            }
        }

        /// <summary>
        /// Parses value of requested key from passed JObject. If key doesnt exist returns empty string.
        /// </summary>
        /// <param name="jObject"></param>
        /// <param name="requestedProperty"></param>
        /// <returns></returns>
        public static string ParseResponseProperty( JObject jObject, string requestedProperty )
        {
            if ( JsonContainsKey( jObject, requestedProperty ) )
            {
                return ( string ) jObject[requestedProperty];
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Parses value of requested key from passed JObject. If key doesnt exist returns empty string.
        /// </summary>
        /// <param name="jObject"></param>
        /// <param name="requestedProperty"></param>
        /// <returns></returns>
        public static string ParseResponsePropertyWithValue( JObject jObject, string requestedProperty )
        {
            if ( JsonContainsKey( jObject, requestedProperty ) )
            {
                return ( string ) jObject[requestedProperty]["value"];
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Check if Json object contains specified key
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool JsonContainsKey( JObject jsonObject, string key )
        {
            foreach ( var item in jsonObject )
            {
                if ( item.Key == key )
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        public enum ApiResult
        {
            Failed,
            Success,
            NothingToSync
        }
    }
}
