using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using church.ccv.CCVCore.HubSpot.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using System.Security.Cryptography;
using static church.ccv.CCVCore.HubSpot.Util.HubSpotConstants;
using static church.ccv.CCVCore.HubSpot.Util.HubSpotApi;

namespace church.ccv.CCVCore.HubSpot.Util
{
    public class HubSpotContactService
    {
        private readonly DefinedTypeCache HubSpotPropertyMap = DefinedTypeCache.Read( church.ccv.Utility.SystemGuids.DefinedType.HUBSPOT_PROPERTY_MAP.AsGuid() );

        /// <summary>
        /// Validate the request came from HubSpot
        /// </summary>
        /// <param name="xHubSpotSignature"></param>
        /// <param name="requestBody"></param>
        /// <returns></returns>
        public static bool ValidateRequest( string xHubSpotSignature, string requestBody )
        {
            string hubSpot_APIKey = GlobalAttributesCache.Value( HubSpotConstants.Api.Key );

            using (SHA256 sha256 = SHA256.Create() )
            {
                // combine the api key and request body and then hash with sha256
                byte[] bytes = Encoding.UTF8.GetBytes( hubSpot_APIKey + requestBody );
                byte[] hash = sha256.ComputeHash( bytes );

                // convert the hash back into a string
                string hashString = "";
                for ( int i = 0; i < hash.Length; i++ )
                {
                    hashString += hash[i].ToString( "x2" );
                }

                // the request is valid if the xHubSpotSignature matches the hashed string
                if ( hashString.ToLower() == xHubSpotSignature.ToLower() )
                {
                    return true;
                }
            }

#if DEBUG
            // return valid if we are in debugging
            return true;
#else
            // default to invalid
            return false;
#endif
        }

#region Process HubSpot Event Methods

        /// <summary>
        /// Process events from HubSpot Webhook
        /// </summary>
        /// <param name="rawEvents"></param>
        /// <returns></returns>
        public static async Task ProcessEvents( string rawEvents )
        {
            // skip if there is nothing to process
            if ( rawEvents.IsNullOrWhiteSpace() )
            {
                return;
            }

            // parse events into JArray
            JArray eventsToProcess = new JArray();
            if ( rawEvents.Substring( 0, 1 ) == "[" )
            {
                // hubspot sent multiple events
                eventsToProcess = JArray.Parse( rawEvents );
            }
            else
            {
                // hubspot sent a single event
                JObject singleEvent = JObject.Parse( rawEvents );
                eventsToProcess.Add( singleEvent );
            }

            // loop through events and process
            foreach ( JObject eventItem in eventsToProcess )
            {
                if ( !HubSpotApi.JsonContainsKey( eventItem, WebHookEventKey.SubscriptionType ) || !HubSpotApi.JsonContainsKey( eventItem, WebHookEventKey.ObjectId ) )
                {
                    // not a valid event, skip to next one
                    continue;
                }
                
                // process the event
                string eventType = ( string ) eventItem[WebHookEventKey.SubscriptionType];
                switch ( eventType )
                {
                    case "contact.creation":
                        await ProcessContactCreation( eventItem );
                        break;
                    case "contact.deletion":
                    case "contact.privacyDeletion":
                        await ProcessContactDeletion( eventItem );
                        break;
                    case "contact.propertyChange":
                        await ProcessPropertyChange( eventItem );
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Process contact creation event
        /// </summary>
        /// <param name="objectToProcess"></param>
        /// <returns></returns>
        private static async Task ProcessContactCreation( JObject objectToProcess )
        {
            RockContext rockContext = new RockContext();            
            Service<HubSpotContact> hubSpotContactService = new Service<HubSpotContact>( rockContext );

            // check for existing contact
            int hubSpotObjectId = ( int ) objectToProcess[WebHookEventKey.ObjectId];
            HubSpotContact hubSpotContact = hubSpotContactService.Queryable().Where( a => a.HubSpotObjectId == hubSpotObjectId ).FirstOrDefault();

            if ( hubSpotContact == null )
            {
                // didnt find an existing contact, create new one
                hubSpotContact = await CreateHubSpotContact( rockContext, hubSpotObjectId );
            }
            else
            {
                // we found a contact, ensure person alias id's are in sync in both Rock and HubSpot
                HttpResponseMessage hubSpotApiResponse = await GetContactFromHubSpot( hubSpotObjectId );
                string responseContent = await hubSpotApiResponse.Content.ReadAsStringAsync();
                if ( hubSpotApiResponse.StatusCode != HttpStatusCode.OK || responseContent.IsNullOrWhiteSpace() )
                {
                    // api call failed
                    throw new Exception( "RequestContact: HubSpot API Error" );
                }

                // parse the response string
                JObject response = JObject.Parse( responseContent );
                JObject responseProperties = ( JObject ) response[WebHookEventKey.Properties];
                string hubSpotRockId = HubSpotApi.ParseResponsePropertyWithValue( responseProperties, ContactPropertyKey.RockId );
                if ( hubSpotRockId.IsNullOrWhiteSpace() )
                {
                    // HubSpot missing rock_id, add to contact in HubSpot 
                    ApiResult apiResult = await HubSpotApi.UpdateContactInHubSpot( hubSpotContact.HubSpotObjectId,
                                                                                   hubSpotContact.PersonAlias.PersonId,
                                                                                   ContactPropertyKey.RockId, 
                                                                                   hubSpotContact.PersonAliasId.ToString() );     
                    
                    if ( apiResult == ApiResult.Failed )
                    {
                        throw new Exception( "UpdateContact: HubSpot API Error" );
                    }
                }
                else if ( hubSpotRockId != hubSpotContact.PersonAliasId.ToString() )
                {
                    // Rock lookup table out of sync with HubSpot, update Rock with person alias id from HubSpot
                    int personAliasId;
                    if ( int.TryParse( hubSpotRockId, out personAliasId ) )
                    {
                        hubSpotContact.PersonAliasId = personAliasId;

                        rockContext.SaveChanges();

                        HubSpotHistoryService.AddHubSpotHistory( SyncDirection.FromHubSpot, 
                                                                 hubSpotContact.PersonAlias.PersonId, 
                                                                 ContactPropertyKey.RockId, 
                                                                 hubSpotContact.PersonAliasId.ToString() );
                    }
                }
            }
        }

        /// <summary>
        /// Process contact deletion event
        /// </summary>
        /// <param name="objectToProcess"></param>
        /// <returns></returns>
        private static async Task ProcessContactDeletion( JObject objectToProcess )
        {
            // lookup contact info to see if this was a delete or a merge
            int hubSpotObjectId = ( int ) objectToProcess[WebHookEventKey.ObjectId];
            HttpResponseMessage hubSpotApiResponse = await HubSpotApi.GetContactFromHubSpot( hubSpotObjectId );

            if ( hubSpotApiResponse.StatusCode == HttpStatusCode.NotFound )
            {
                // 404 Not Found response means contact was deleted
                DeleteHubSpotContactInRock( hubSpotObjectId );
                return;
            }

            // Did not get 404 response, check if this is a merge
            string responseContent = await hubSpotApiResponse.Content.ReadAsStringAsync();
            if ( responseContent.IsNullOrWhiteSpace() )
            {
                // empty response, nothing to do
                return;
            }

            // parse the response string
            JObject response = JObject.Parse( responseContent );
            JObject responseProperties = ( JObject ) response[WebHookEventKey.Properties];

            // check for merged vids
            string hubSpotRockId = HubSpotApi.ParseResponsePropertyWithValue( responseProperties, ContactPropertyKey.RockId );
            string mergedVidsValue = HubSpotApi.ParseResponseProperty( response, WebHookEventKey.MergedVids);
            List<string> mergedVids = mergedVidsValue.Split( ',' ).ToList();
            if ( mergedVids.Count <= 0 )
            {
                // no merged id's, nothing to do
                return;
            }

            RockContext rockContext = new RockContext();

            // load the person alias
            PersonAlias personAlias = null;
            int personAliasId;
            if ( int.TryParse( hubSpotRockId, out personAliasId ) )
            {
                personAlias = new PersonAliasService( rockContext ).Get( personAliasId );
            }

            if ( personAlias == null )
            {
                // create a new person
                string firstName = HubSpotApi.ParseResponsePropertyWithValue( responseProperties, ContactPropertyKey.FirstName );
                string lastName = HubSpotApi.ParseResponsePropertyWithValue( responseProperties, ContactPropertyKey.LastName );
                string email = HubSpotApi.ParseResponsePropertyWithValue( responseProperties, ContactPropertyKey.Email );
                string dateOfBirth = HubSpotApi.ParseResponsePropertyWithValue( responseProperties, ContactPropertyKey.DateOfBirth );

                // we need at least an email address or first and last name
                if ( email.IsNotNullOrWhiteSpace() || ( firstName.IsNotNullOrWhiteSpace() && lastName.IsNotNullOrWhiteSpace() ) )
                {
                    personAlias = CreatePersonInRock( rockContext, email, firstName, lastName, dateOfBirth );
                }  
            }

            if ( personAlias == null )
            {
                // if we still dont have person alias, something went wrong
                throw new Exception( "ContactDeletionMerge: Failed to load Person" );
            }
                        
            // update all hub spot id's in rock to point to same person alias id
            Service<HubSpotContact> hubSpotContactService = new Service<HubSpotContact>( rockContext );

            foreach ( string hubSpotVid in mergedVids )
            {
                List<HubSpotContact> hubSpotContacts = hubSpotContactService.Queryable().Where( a => a.HubSpotObjectId.ToString() == hubSpotVid ).ToList();
                foreach ( HubSpotContact hubSpotContact in hubSpotContacts )
                {
                    hubSpotContact.PersonAliasId = personAlias.Id;
                    hubSpotContact.PersonAlias = personAlias;
                }
            }

            rockContext.SaveChanges();
        }

        /// <summary>
        /// Process property change event from HubSpot
        /// </summary>
        /// <param name="hubSpotContact"></param>
        /// <param name="objectToProcess"></param>
        private static async Task ProcessPropertyChange( JObject objectToProcess )
        {
            // ensure the event has required keys
            if ( !HubSpotApi.JsonContainsKey( objectToProcess, WebHookEventKey.PropertyName ) || !HubSpotApi.JsonContainsKey( objectToProcess, WebHookEventKey.PropertyValue ) )
            {
                // missing required keys skip
                throw new Exception( "PropertyChange: Missing property name or value" );
            }
                       
            RockContext rockContext = new RockContext();            
            Service<HubSpotContact> hubSpotContactService = new Service<HubSpotContact>( rockContext );

            // check for existing contact
            int hubSpotObjectId = ( int ) objectToProcess[WebHookEventKey.ObjectId];
            HubSpotContact hubSpotContact = hubSpotContactService.Queryable().Where( a => a.HubSpotObjectId == hubSpotObjectId ).FirstOrDefault();

            if ( hubSpotContact == null )
            {
                // didnt find an existing contact, create new one
                hubSpotContact = await CreateHubSpotContact( rockContext, hubSpotObjectId );
            }
                        
            // ensure we have a hubSpotContact and the PersonAlias property is populated 
            if ( hubSpotContact == null || hubSpotContact.PersonAlias == null )
            {
                return;
            }

            // dont update if its a staff member
            var staffMembers = new GroupService( rockContext ).Get( Rock.SystemGuid.Group.GROUP_STAFF_MEMBERS.AsGuid() ).Members.Select( a => a.PersonId );
            if ( staffMembers.Contains( hubSpotContact.PersonAlias.PersonId ) )
            {
                return;
            }

            string propertyName = ( string ) objectToProcess[WebHookEventKey.PropertyName];
            string propertyValue = ( string ) objectToProcess[WebHookEventKey.PropertyValue];

            switch ( propertyName )
            {
                case ContactPropertyKey.FirstName:
                case ContactPropertyKey.LastName:
                case ContactPropertyKey.Email:
                case ContactPropertyKey.DateOfBirth:
                case ContactPropertyKey.Phone:
                case ContactPropertyKey.MobilePhone:
                case ContactPropertyKey.Address:
                case ContactPropertyKey.City:
                case ContactPropertyKey.State:
                case ContactPropertyKey.Zip:
                case ContactPropertyKey.Country:
                case ContactPropertyKey.EmailOptOut:
                {
                    await UpdatePersonInRock( hubSpotContact, propertyName, propertyValue );
                    break;
                }
                default:
                {
                    // if no person field was specified, then this is a custom attribute
                    // load the hubSpotPropertyMap to determine if we have a mapped attribute
                    DefinedTypeCache hubSpotPropertyMap = DefinedTypeCache.Read( church.ccv.Utility.SystemGuids.DefinedType.HUBSPOT_PROPERTY_MAP.AsGuid() );
                    if ( hubSpotPropertyMap == null )
                    {
                        throw new Exception( "PropertyUpdate: Failed to load property map" );
                    }
                    
                    var hubSpotProperty = hubSpotPropertyMap.DefinedValues.Where( a => a.Value == propertyName ).FirstOrDefault();
                    if ( hubSpotProperty != null )
                    {
                        // found mapped property
                        // get the ExcludeStaff and RockAttribute attribute
                        Guid rockAttributeGuid = hubSpotProperty.GetAttributeValue( ContactPropertyMapKey.RockAttribute ).AsGuid();
                        bool excludeStaff = hubSpotProperty.GetAttributeValue( ContactPropertyMapKey.ExcludeStaff ).AsBoolean();
                        if ( excludeStaff == false && rockAttributeGuid != Guid.Empty )
                        {
                            // load the attribute
                            AttributeCache rockAttribute = AttributeCache.Read( rockAttributeGuid );

                            UpdatePersonAttributeValueInRock( hubSpotContact, rockAttribute, propertyName, propertyValue );
                        }
                    }

                    break;
                }
            }
        }

#endregion

#region Rock HubSpot Entity Methods

        /// <summary>
        /// Create a new HubSpotContact entity.
        /// </summary>
        /// <param name="rockContext"></param>
        /// <param name="hubSpotObjectId"></param>
        /// <returns></returns>
        private static async Task<HubSpotContact> CreateHubSpotContact( RockContext rockContext, int hubSpotObjectId )
        {
            HubSpotContact hubSpotContact;

            // get the contact info from HubSpot
            HttpResponseMessage contactRequestApiResponse = await HubSpotApi.GetContactFromHubSpot( hubSpotObjectId );
            string contactRequestResponseContent = await contactRequestApiResponse.Content.ReadAsStringAsync();

            if ( contactRequestApiResponse.StatusCode != HttpStatusCode.OK || contactRequestResponseContent.IsNullOrWhiteSpace() )
            {
                // api call failed or response empty
                throw new Exception( "RequestContact: HubSpot API Failure" );
            }

            // parse the response string
            JObject response = JObject.Parse( contactRequestResponseContent );
            JObject responseProperties = ( JObject ) response[WebHookEventKey.Properties];

            string firstName = HubSpotApi.ParseResponsePropertyWithValue( responseProperties, ContactPropertyKey.FirstName );
            string lastName = HubSpotApi.ParseResponsePropertyWithValue( responseProperties, ContactPropertyKey.LastName );
            string email = HubSpotApi.ParseResponsePropertyWithValue( responseProperties, ContactPropertyKey.Email );
            string dateOfBirth = HubSpotApi.ParseResponsePropertyWithValue( responseProperties, ContactPropertyKey.DateOfBirth );
            string hubSpotRockId = HubSpotApi.ParseResponsePropertyWithValue( responseProperties, ContactPropertyKey.RockId );

            // get/create PersonAlias
            PersonAlias personAlias = null;
            int personAliasId;
            if ( int.TryParse( hubSpotRockId, out personAliasId ) )
            {
                personAlias = new PersonAliasService( rockContext ).Get( personAliasId );
            }

            if ( personAlias == null )
            {
                // look for a matching person in rock by first name, last name, and email address
                Person person = new PersonService( rockContext ).FindPerson( firstName, lastName, email, false, false, false );

                if ( person != null )
                {
                    // single match found
                    personAlias = person.PrimaryAlias;
                }
            }

            if ( personAlias == null )
            {
                // did not have a valid rock_id or didnt find matching person in Rock, create new person
                // we need at least an email address or first and last name
                if ( email.IsNotNullOrWhiteSpace() || ( firstName.IsNotNullOrWhiteSpace() && lastName.IsNotNullOrWhiteSpace() ) )
                {
                    personAlias = CreatePersonInRock( rockContext, email, firstName, lastName, dateOfBirth );
                }              
            }        
            
            // if we dont have person alias at this point, something went wrong
            if ( personAlias == null )
            {
                throw new Exception( "CreateContact: Failed to load person alias" );
            }

            // update rock id in HubSpot
            if ( hubSpotRockId != personAlias.Id.ToString() )
            {
                // HubSpot missing rock_id, add to contact in HubSpot 
                ApiResult apiResult = await HubSpotApi.UpdateContactInHubSpot( hubSpotObjectId,
                                                                               personAlias.PersonId,
                                                                               ContactPropertyKey.RockId,
                                                                               personAlias.Id.ToString() );

                if ( apiResult == ApiResult.Failed )
                {
                    throw new Exception( "UpdateContact: HubSpot API Error" );
                }
            }

            hubSpotContact = new HubSpotContact
            {
                PersonAlias = personAlias,
                PersonAliasId = personAlias.Id,
                HubSpotObjectId = hubSpotObjectId,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Guid = Guid.NewGuid(),
                CreatedDateTime = DateTime.Now,
                ModifiedDateTime = DateTime.Now,
                LastSyncDateTime = DateTime.Now
            };

            Service<HubSpotContact> hubSpotContactService = new Service<HubSpotContact>( rockContext );
            hubSpotContactService.Add( hubSpotContact );

            rockContext.SaveChanges();

            HubSpotHistoryService.AddHubSpotHistory( "Linked new HubSpotContact: " + hubSpotObjectId.ToString(), personAlias.PersonId );

            return hubSpotContact;
        }

        /// <summary>
        /// Delete a HubSpot contact in Rock
        /// </summary>
        /// <param name="hubSpotObjectId"></param>
        private static void DeleteHubSpotContactInRock( int hubSpotObjectId )
        {
            // get list of HubSpotContacts
            RockContext rockContext = new RockContext();
            Service<HubSpotContact> hubSpotContactService = new Service<HubSpotContact>( rockContext );

            List<HubSpotContact> hubSpotContacts = hubSpotContactService.Queryable().Where( a => a.HubSpotObjectId == hubSpotObjectId ).ToList();
            if ( hubSpotContacts.Count == 0 )
            {
                // nothing found, nothing to do
                return;
            }

            // remove each contact
            foreach ( HubSpotContact hubSpotContact in hubSpotContacts )
            {
                RockContext rockContextDelete = new RockContext();
                Service<HubSpotContact> hubSpotContactDeleteService = new Service<HubSpotContact>( rockContextDelete );

                HubSpotHistoryService.AddHubSpotHistory( "Removed HubSpotConact Link: " + hubSpotContact.HubSpotObjectId, hubSpotContact.PersonAlias.PersonId );

                hubSpotContactDeleteService.Delete( hubSpotContact );

                rockContextDelete.SaveChanges();
            }
        }

        #endregion

        #region Rock Person Methods

        /// <summary>
        /// Returns an attribute key and value of a person attribute that is mapped to a hubspot property
        /// </summary>
        /// <param name="person"></param>
        /// <param name="attributeName"></param>
        /// <param name="hubSpotPropertyKey"></param>
        /// <param name="attributeValue"></param>
        public void GetValueFromMappedPersonAttribute( Person person, string attributeName, out string hubSpotPropertyKey, out string attributeValue )
        {
            // default to empty
            hubSpotPropertyKey = string.Empty;
            attributeValue = string.Empty;

            // loop through mapped properties and look for a match
            foreach ( var mappedProperty in HubSpotPropertyMap.DefinedValues )
            {
                // load the Rock attribute from the mapped property
                Guid mappedAttributeGuid = mappedProperty.GetAttributeValue( ContactPropertyMapKey.RockAttribute ).AsGuid();
                AttributeCache mappedAttribute = AttributeCache.Read( mappedAttributeGuid );
                if ( mappedAttribute != null && mappedAttribute.Name == attributeName )
                {
                    // mapped attribute name matches 
                    person.LoadAttributes();
                    string unformattedAttributeValue = person.GetAttributeValue( mappedAttribute.Key );
                    if ( unformattedAttributeValue.IsNotNullOrWhiteSpace() )
                    {
                        // format the attribute value as needed
                        switch ( mappedAttribute.FieldType.Name )
                        {
                            case "Boolean":
                                attributeValue = unformattedAttributeValue.ToLower().Contains("t") || 
                                                 unformattedAttributeValue.ToLower().Contains("y") 
                                                 ? "true" 
                                                 : "false";
                                break;
                            default:
                                attributeValue = unformattedAttributeValue;
                                break;
                        }

                        // the hubspot key is the value of the mapped property
                        hubSpotPropertyKey = mappedProperty.Value;
                    }

                    return;
                }
            }
        }

        /// <summary>
        /// Create and return a new person in Rock
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="email"></param>
        /// <param name="dateOfBirth"></param>
        /// <returns></returns>
        private static PersonAlias CreatePersonInRock( RockContext rockContext, string email, string firstName, string lastName, string dateOfBirth )
        {
            // create new person, if we dont have a first and last name, use email address as first name
            Person newPerson = new Person()
            {
                FirstName = firstName.FixCase(),
                LastName = lastName.FixCase(),
                Email = email,
                ConnectionStatusValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT.AsGuid() ).Id,
                RecordTypeValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id,
                RecordStatusValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() ).Id
            };

            PersonService.SaveNewPerson( newPerson, rockContext );

            // date of birth in HubSpot is a string field, we can handle month/day or month/day/year using a / or -
            UpdatePersonDateOfBirth( newPerson, dateOfBirth );

            // add message in person history that this came from HubSpot Service
            HubSpotHistoryService.AddRockPersonHistory( "Created by HubSpot Service", newPerson.Id );

            rockContext.SaveChanges();

            return newPerson.PrimaryAlias;
        }

        /// <summary>
        /// Update a person in Rock
        /// </summary>
        /// <param name="hubSpotContact"></param>
        /// <param name="personField"></param>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        private static async Task UpdatePersonInRock( HubSpotContact hubSpotContact, string personField, string propertyValue )
        {
            // get the person object
            RockContext rockContext = new RockContext();
            Person person = new PersonService( rockContext ).Get( hubSpotContact.PersonAlias.Person.Guid );
            bool addHistoryEntry = true;

            switch ( personField )
            {
                case ContactPropertyKey.FirstName:
                {
                    person.FirstName = propertyValue.FixCase();
                    person.NickName = propertyValue.FixCase();
                    break;
                }
                case ContactPropertyKey.LastName:
                {
                    person.LastName = propertyValue.FixCase();
                    break;
                }
                case ContactPropertyKey.Email:
                {
                    person.Email = propertyValue.ToLower();
                    break;
                }
                case ContactPropertyKey.DateOfBirth:
                {
                    UpdatePersonDateOfBirth( person, propertyValue );
                    break;
                }
                case ContactPropertyKey.EmailOptOut:
                {
                    if ( propertyValue == "true" )
                    {
                        person.EmailPreference = EmailPreference.NoMassEmails;
                        HubSpotHistoryService.AddHubSpotHistory( SyncDirection.FromHubSpot,
                                                                    hubSpotContact.PersonAlias.PersonId,
                                                                    personField,
                                                                    EmailPreference.NoMassEmails.ToString() );
                    }
                    else
                    {
                        person.EmailPreference = EmailPreference.EmailAllowed;
                        HubSpotHistoryService.AddHubSpotHistory( SyncDirection.FromHubSpot,
                                                                    hubSpotContact.PersonAlias.PersonId,
                                                                    personField,
                                                                    EmailPreference.EmailAllowed.ToString() );
                    }
                    addHistoryEntry = false;
                    break;
                }
                case ContactPropertyKey.Phone:
                case ContactPropertyKey.MobilePhone:
                {
                    // clean number
                    string newNumber = PhoneNumber.CleanNumber( propertyValue );

                    // ensure we have a valid number before proceeding
                    if ( newNumber.Length >= 7 && newNumber.Length <= 20 && !newNumber.StartsWith( "911" ) && !newNumber.StartsWith( "1911" ) )
                    {
                        // set phone type, default to mobile phone type with messaging enabled
                        DefinedValueCache phoneType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
                        bool isMessagingEnabled = true;

                        if ( personField == ContactPropertyKey.Phone )
                        {
                            // hubspot sent home phone, change phone type and disable messaging
                            phoneType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() );
                            isMessagingEnabled = false;
                        }

                        person.UpdatePhoneNumber( phoneType.Id, "", newNumber, isMessagingEnabled, false, rockContext );
                    }

                    break;
                }
                case ContactPropertyKey.Address:
                case ContactPropertyKey.City:
                case ContactPropertyKey.State:
                case ContactPropertyKey.Zip:
                case ContactPropertyKey.Country:
                {
                    // we dont update the address using the information passed in the webhook event
                    // instead we query HubSpot api to get full address and then update Rock if needed
                    await UpdatePersonAddress( person, hubSpotContact.HubSpotObjectId, rockContext );
                    addHistoryEntry = false;
                    break;
                }
                default:
                    break;
            }

            // add a hubspot history entry
            if ( addHistoryEntry == true )
            {
                HubSpotHistoryService.AddHubSpotHistory( SyncDirection.FromHubSpot, hubSpotContact.PersonAlias.PersonId, personField, propertyValue );
            }

            rockContext.SaveChanges();
        }

        /// <summary>
        /// Update person attribute value in Rock
        /// </summary>
        /// <param name="hubSpotContact"></param>
        /// <param name="rockAttribute"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param> 
        private static void UpdatePersonAttributeValueInRock( HubSpotContact hubSpotContact, AttributeCache rockAttribute, string propertyName, string propertyValue )
        {
            RockContext rockContext = new RockContext();
            AttributeValueService attributeValueService = new AttributeValueService( rockContext );
            AttributeValue attributeValue = attributeValueService.GetByAttributeIdAndEntityId( rockAttribute.Id, hubSpotContact.PersonAlias.Person.Id );

            if ( attributeValue == null )
            {
                // attribute value doesnt exist, create a new one.
                attributeValue = new AttributeValue
                {
                    AttributeId = rockAttribute.Id,
                    EntityId = hubSpotContact.PersonAlias.Person.Id
                };
                attributeValueService.Add( attributeValue );
            }

            if ( rockAttribute.FieldType.Name == "Boolean" )
            {
                attributeValue.Value = propertyValue == "true" ? "True" : "False" ;
            }
            else
            {
                // default action update value as is
                attributeValue.Value = propertyValue;
            }

            rockContext.SaveChanges();

            // create a person history entry
            HubSpotHistoryService.AddHubSpotHistory( SyncDirection.FromHubSpot, hubSpotContact.PersonAlias.PersonId, propertyName, propertyValue );
        }

        /// <summary>
        /// Add or Update a person's address
        /// </summary>
        /// <param name="person"></param>
        /// <param name="hubSpotObjectId"></param>
        /// <param name="rockContext"></param>
        private static async Task UpdatePersonAddress( Person person, int hubSpotObjectId, RockContext rockContext )
        {
            // we dont update the address using the information passed in the webhook event
            // instead we query HubSpot api to get full address and then update Rock if needed                       
            Location newAddressLocation = await HubSpotApi.GetContactAddressFromHubSpot( rockContext, hubSpotObjectId );
            if ( newAddressLocation == null )
            {
                // unable to get valid Location, skip update
                return;
            }

            Group family = person.GetFamily();
            if ( family == null )
            {
                // missing family, skip update
                throw new Exception( "Error loading family" );
            }

            GroupLocationService groupLocationService = new GroupLocationService( rockContext );
            DefinedValueCache homeAddressType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() );
            GroupLocation homeAddress = groupLocationService.Queryable()
                .Where( a => a.GroupId == family.Id && a.GroupLocationTypeValueId == homeAddressType.Id )
                .FirstOrDefault();

            if ( homeAddress == null )
            {
                // family doesnt have current address, create a new one
                homeAddress = new GroupLocation
                {
                    GroupLocationTypeValueId = homeAddressType.Id,
                    GroupId = family.Id,
                    Location = newAddressLocation,
                    IsMailingLocation = true,
                    IsMappedLocation = true
                };

                groupLocationService.Add( homeAddress );
            }
            else
            {
                if ( homeAddress.Location.Guid == newAddressLocation.Guid )
                {
                    // locations are the same, skip update
                    return;
                }

                // copy current home address to previous address
                DefinedValueCache previousAddressType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_PREVIOUS.AsGuid() );
                GroupLocation previousAddress = new GroupLocation
                {
                    GroupLocationTypeValueId = previousAddressType.Id,
                    GroupId = family.Id,
                    Location = homeAddress.Location
                };
                groupLocationService.Add( previousAddress );

                // update current home location to new location
                homeAddress.Location = newAddressLocation;

                // since there can only be one mapped location, set the other locations to not mapped
                if ( homeAddress.IsMappedLocation )
                {
                    var groupLocations = groupLocationService.Queryable()
                        .Where( a => a.GroupId == family.Id && a.Id != homeAddress.Id )
                        .ToList();

                    foreach ( var grouplocation in groupLocations )
                    {
                        grouplocation.IsMappedLocation = false;
                        grouplocation.IsMailingLocation = false;
                    }
                }
            }

            // create a person history entry
            List<string> changes = new List<string>
            {
                HubSpotHistoryService.CreateChangeEntry( SyncDirection.FromHubSpot, ContactPropertyKey.Address, newAddressLocation.Street1 ),
                HubSpotHistoryService.CreateChangeEntry( SyncDirection.FromHubSpot, ContactPropertyKey.City, newAddressLocation.City ),
                HubSpotHistoryService.CreateChangeEntry( SyncDirection.FromHubSpot, ContactPropertyKey.State, newAddressLocation.State ),
                HubSpotHistoryService.CreateChangeEntry( SyncDirection.FromHubSpot, ContactPropertyKey.Zip, newAddressLocation.PostalCode ),
                HubSpotHistoryService.CreateChangeEntry( SyncDirection.FromHubSpot, ContactPropertyKey.Country, newAddressLocation.Country ),
            };
            HubSpotHistoryService.AddHubSpotHistory( changes, person.Id );

            rockContext.SaveChanges();
        }
        
        /// <summary>
        /// Parse a date of birth string from hubspot and update respective date of birth person fields
        /// </summary>
        /// <param name="person"></param>
        /// <param name="newDateOfBirth"></param>
        private static void UpdatePersonDateOfBirth( Person person, string newDateOfBirth )
        {
            if (  !newDateOfBirth.Contains( "/" ) && !newDateOfBirth.Contains( "-" ) )
            {
                // new date of birth string is not in expected format, skip update
                throw new Exception( "Date of Birth string is not valid format" );
            }

            // split new date of birth into array
            string[] birthDateValues = new string[] { };                
            if ( newDateOfBirth.Contains( "/" ) )
            {
                birthDateValues = newDateOfBirth.Split( '/' );
            }
            else if ( newDateOfBirth.Contains( "-" ) )
            {
                birthDateValues = newDateOfBirth.Split( '-' );
            }
                
            // update relavant date of birth fields
            if ( birthDateValues.Length == 2 || birthDateValues.Length == 3 )
            {
                // month
                int birthMonth;
                if ( int.TryParse( birthDateValues[0], out birthMonth ) )
                {
                    person.BirthMonth = birthMonth;
                }
                // day
                int birthDay;
                if ( int.TryParse( birthDateValues[1], out birthDay ) )
                {
                    person.BirthDay = birthDay;
                }
            }

            if ( birthDateValues.Length == 3 && birthDateValues[2].Length == 4 )
            {
                // year
                int birthYear;
                if ( int.TryParse( birthDateValues[2], out birthYear ) )
                {
                    person.BirthYear = birthYear;
                }
            }            
        }

        #endregion

    }
}
