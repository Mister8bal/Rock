using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace church.ccv.CCVCore.HubSpot.Util
{
    public class HubSpotContactService
    {
        /// <summary>
        /// Validate the request came from HubSpot
        /// </summary>
        /// <param name="xHubSpotSignature"></param>
        /// <param name="requestBody"></param>
        /// <returns></returns>
        public static bool ValidateRequest( string xHubSpotSignature, string requestBody )
        {
            string hubSpot_APIKey = GlobalAttributesCache.Value( "HubSpotAPIKey" );

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
                if ( !JsonContainsKey( eventItem, "subscriptionType" ) || !JsonContainsKey( eventItem, "objectId" ) )
                {
                    // not a valid event, skip to next one
                    continue;
                }
                
                // process the event
                string eventType = ( string ) eventItem["subscriptionType"];
                switch ( eventType )
                {
                    case "contact.creation":
                        await ProcessContactCreation( eventItem );
                        break;
                    case "contact.deletion":
                        await ProcessContactDeletion( eventItem );
                        break;
                    case "contact.privacyDeletion":
                        // need to determine what is legally needed for privacy deletion
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
            int hubSpotObjectId = ( int ) objectToProcess["objectId"];
            HubSpotContact hubSpotContact = hubSpotContactService.Queryable().Where( a => a.HubSpotObjectId == hubSpotObjectId ).FirstOrDefault();

            if ( hubSpotContact == null )
            {
                // didnt find an existing contact, create new one
                hubSpotContact = await CreateHubSpotContact( rockContext, hubSpotObjectId );
            }
            else
            {
                // we found a contact, ensure person alias id's are in sync in both Rock and HubSpot
                List<string> contactProperties = new List<string>
                {
                    "rock_id"
                };
                HttpResponseMessage hubSpotApiResponse = await RequestContactFromHubSpot( hubSpotObjectId, contactProperties );
                string responseContent = await hubSpotApiResponse.Content.ReadAsStringAsync();

                if ( hubSpotApiResponse.StatusCode != HttpStatusCode.OK || responseContent.IsNullOrWhiteSpace() )
                {
                    // api call failed
                    throw new Exception( "RequestContact: HubSpot API Error" );
                }

                // parse the response string
                JObject response = JObject.Parse( responseContent );
                JObject responseProperties = ( JObject ) response["properties"];

                string hubSpotRockId = string.Empty;
                if ( JsonContainsKey( responseProperties, "rock_id" ) )
                {
                    hubSpotRockId = ( string ) responseProperties["rock_id"]["value"];
                }

                if ( hubSpotRockId.IsNullOrWhiteSpace() )
                {
                    // HubSpot missing rock_id, add to contact in HubSpot
                    Dictionary<string, string> propertiesToUpdate = new Dictionary<string, string>
                    {
                        { "rock_id", hubSpotContact.PersonAliasId.ToString() }
                    };

                    HttpResponseMessage updateContactApiResponse = await UpdateContactInHubSpot( hubSpotObjectId, propertiesToUpdate );
                }
                else if ( hubSpotRockId != hubSpotContact.PersonAliasId.ToString() )
                {
                    // Rock lookup table out of sync with HubSpot, update Rock with person alias id from HubSpot
                    if ( int.TryParse( hubSpotRockId, out int personAliasId ) )
                    {
                        hubSpotContact.PersonAliasId = personAliasId;

                        rockContext.SaveChanges();
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
            int hubSpotObjectId = ( int ) objectToProcess["objectId"];
            List<string> contactProperties = new List<string>
            {
                "rock_id",
                "firstname",
                "lastname",
                "email",
                "date_of_birth"
            };
            HttpResponseMessage hubSpotApiResponse = await RequestContactFromHubSpot( hubSpotObjectId, contactProperties );

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
            JObject responseProperties = ( JObject ) response["properties"];

            // check for merged vids
            List<string> mergedVids = new List<string>();
            if ( JsonContainsKey( response, "merged-vids" ) )
            {
                string mergedVidsValue = ( string ) response["merged-vids"];
                mergedVids = mergedVidsValue.Split( ',' ).ToList();
            }

            string hubSpotRockId = string.Empty;
            if ( JsonContainsKey( responseProperties, "rock_id" ) )
            {
                hubSpotRockId = ( string ) responseProperties["rock_id"]["value"];
            }

            if ( mergedVids.Count <= 0 )
            {
                // no merged id's, nothing to do
                return;
            }

            RockContext rockContext = new RockContext();

            // load the person alias
            PersonAlias personAlias = null;
            if ( int.TryParse( hubSpotRockId, out int personAliasId ) )
            {
                personAlias = new PersonAliasService( rockContext ).Get( personAliasId );
            }

            if ( personAlias == null )
            {
                // create a new person
                string firstName = string.Empty;
                if ( JsonContainsKey( responseProperties, "firstname" ) )
                {
                    firstName = ( string ) responseProperties["firstname"]["value"];
                }

                string lastName = string.Empty;
                if ( JsonContainsKey( responseProperties, "lastname" ) )
                {
                    lastName = ( string ) responseProperties["lastname"]["value"];
                }

                string email = string.Empty;
                if ( JsonContainsKey( responseProperties, "email" ) )
                {
                    email = ( string ) responseProperties["email"]["value"];
                }

                string dateOfBirth = string.Empty;
                if ( JsonContainsKey( responseProperties, "date_of_birth" ) )
                {
                    dateOfBirth = ( string ) responseProperties["date_of_birth"]["value"];
                }

                // we need at least an email address or first and last name
                if ( email.IsNotNullOrWhiteSpace() || ( firstName.IsNotNullOrWhiteSpace() && lastName.IsNotNullOrWhiteSpace() ) )
                {
                    personAlias = CreatePersonInRock( rockContext, email, firstName, lastName, dateOfBirth );
                }  
            }

            if ( personAlias == null )
            {
                // if we still dont have person alias, something went wrong
                return;
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
            if ( !JsonContainsKey( objectToProcess, "propertyName") || !JsonContainsKey( objectToProcess, "propertyValue" ) )
            {
                // missing required keys skip
                throw new Exception( "PropertyChange: Missing property name or value" );
            }
                       
            RockContext rockContext = new RockContext();            
            Service<HubSpotContact> hubSpotContactService = new Service<HubSpotContact>( rockContext );

            // check for existing contact
            int hubSpotObjectId = ( int ) objectToProcess["objectId"];
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

            string propertyName = ( string ) objectToProcess["propertyName"];
            string propertyValue = ( string ) objectToProcess["propertyValue"];

            switch ( propertyName )
            {
                case "firstname":
                case "lastname":
                case "email":
                case "date_of_birth":
                case "phone":
                case "mobilephone":
                case "address":
                case "city":
                case "state":
                case "zip":
                case "country":
                case "hs_email_optout":
                {
                    await UpdatePersonInRock( hubSpotContact, propertyName, propertyValue );
                    break;
                }
                default:
                {
                    // if no person field was specified, then this is a custom attribute
                    // load the hubSpotPropertyMap to determine if we have a mapped attribute
                    var hubSpotPropertyMap = DefinedTypeCache.Read( church.ccv.Utility.SystemGuids.DefinedType.HUBSPOT_PROPERTY_MAP.AsGuid() );
                    if ( hubSpotPropertyMap != null )
                    {
                        var hubSpotProperty = hubSpotPropertyMap.DefinedValues.Where( a => a.Value == propertyName ).FirstOrDefault();

                        if ( hubSpotProperty != null )
                        {
                            // found a property map
                            string attributeIdValue = hubSpotProperty.GetAttributeValue( "AttributeId" );
                            string attributeFieldType = hubSpotProperty.GetAttributeValue( "AttributeFieldType" );
                            bool excludeStaff = hubSpotProperty.GetAttributeValue( "ExcludeStaff" ).AsBoolean();
                            if ( excludeStaff == false && attributeIdValue.IsNotNullOrWhitespace() )
                            {
                                if ( int.TryParse( attributeIdValue, out int attributeId ) )
                                {
                                    UpdatePersonAttributeValueInRock( hubSpotContact, attributeId, attributeFieldType, propertyValue );
                                }
                            }
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
            HubSpotContact hubSpotContact = null;

            // get the contact info from HubSpot
            List<string> contactProperties = new List<string>
            {
                "rock_id",
                "firstname",
                "lastname",
                "email",
                "date_of_birth"
            };
            HttpResponseMessage contactRequestApiResponse = await RequestContactFromHubSpot( hubSpotObjectId, contactProperties );
            string contactRequestResponseContent = await contactRequestApiResponse.Content.ReadAsStringAsync();

            if ( contactRequestApiResponse.StatusCode != HttpStatusCode.OK || contactRequestResponseContent.IsNullOrWhiteSpace() )
            {
                // api call failed or response empty
                throw new Exception( "RequestContact: HubSpot API Failure" );
            }

            // HubSpot response is not guarenteed to have all the requested properties in its response.
            // Like the attribute value table, a HubSpot property does not get created for a contact
            // until a value is entered. We need to check for the keys in the response before we try
            // to assign their values to avoid null exceptions.

            // parse the response string
            JObject response = JObject.Parse( contactRequestResponseContent );
            JObject responseProperties = ( JObject ) response["properties"];

            string firstName = string.Empty;
            if ( JsonContainsKey( responseProperties, "firstname" ) )
            {
                firstName = ( string ) responseProperties["firstname"]["value"];
            }

            string lastName = string.Empty;
            if ( JsonContainsKey( responseProperties, "lastname" ) )
            {
                lastName = ( string ) responseProperties["lastname"]["value"];
            }

            string email = string.Empty;
            if ( JsonContainsKey( responseProperties, "email" ) )
            {
                email = ( string ) responseProperties["email"]["value"];
            }

            string dateOfBirth = string.Empty;
            if ( JsonContainsKey( responseProperties, "date_of_birth" ) )
            {
                dateOfBirth = ( string ) responseProperties["date_of_birth"]["value"];
            }

            // get/create PersonAlias
            PersonAlias personAlias = null;

            string hubSpotRockId = string.Empty;
            if ( JsonContainsKey( responseProperties, "rock_id" ) )
            {
                hubSpotRockId = ( string ) responseProperties["rock_id"]["value"];
            }

            if ( int.TryParse( hubSpotRockId, out int personAliasId ) )
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
                return null;
            }

            // update rock id in HubSpot
            if ( hubSpotRockId != personAlias.Id.ToString() )
            {
                Dictionary<string, string> propertiesToUpdate = new Dictionary<string, string>
                {
                    { "rock_id", personAlias.Id.ToString() }
                };

                HttpResponseMessage updateContactApiResponse = await UpdateContactInHubSpot( hubSpotObjectId, propertiesToUpdate );

                if ( updateContactApiResponse.StatusCode != HttpStatusCode.NoContent )
                {
                    // should this turn into while loop so that it retries a couple times if the update fails?!?
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

                hubSpotContactDeleteService.Delete( hubSpotContact );

                rockContextDelete.SaveChanges();
            }
        }

#endregion

#region Rock Person Methods

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
            PersonService personService = new PersonService( rockContext );

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
            var changes = new List<string>
            {
                "Created by HubSpot Service"
            };
            HistoryService.AddChanges( rockContext, typeof( Person ), Rock.SystemGuid.Category.HISTORY_PERSON_DEMOGRAPHIC_CHANGES.AsGuid(), newPerson.Id, changes );

            // add person to the HubSpot Prospects group
            Group hubSpotProspects = new GroupService( rockContext ).Get( church.ccv.Utility.SystemGuids.Group.GROUP_HUBSPOT_PROSPECTS.AsGuid() );
            GroupTypeRole defaultGroupRole = hubSpotProspects.GroupType.DefaultGroupRole;
            GroupMember groupMember = new GroupMember()
            {
                GroupId = hubSpotProspects.Id,
                PersonId = newPerson.Id,
                GroupRoleId = defaultGroupRole.Id,
                GroupMemberStatus = GroupMemberStatus.Active
            };
            hubSpotProspects.Members.Add( groupMember );

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

            switch ( personField )
            {
                case "firstname":
                {
                    person.FirstName = propertyValue.FixCase();
                    person.NickName = propertyValue.FixCase();
                    break;
                }
                case "lastname":
                {
                    person.LastName = propertyValue.FixCase();
                    break;
                }
                case "email":
                {
                    person.Email = propertyValue.ToLower();
                    break;
                }
                case "date_of_birth":
                {
                    UpdatePersonDateOfBirth( person, propertyValue );
                    break;
                }
                case "hs_email_optout":
                {
                    if ( propertyValue == "true" )
                    {
                        person.EmailPreference = EmailPreference.NoMassEmails;
                    }
                    else
                    {
                        person.EmailPreference = EmailPreference.EmailAllowed;
                    }
                    break;
                }
                case "phone":
                case "mobilephone":
                {
                    // clean number
                    string newNumber = PhoneNumber.CleanNumber( propertyValue );

                    // ensure we have a valid number before proceeding
                    if ( newNumber.Length >= 7 && newNumber.Length <= 20 && !newNumber.StartsWith( "911" ) && !newNumber.StartsWith( "1911" ) )
                    {
                        // set phone type, default to mobile phone type with messaging enabled
                        DefinedValueCache phoneType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
                        bool isMessagingEnabled = true;

                        if ( personField == "phone" )
                        {
                            // hubspot sent home phone, change phone type and disable messaging
                            phoneType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() );
                            isMessagingEnabled = false;
                        }

                        person.UpdatePhoneNumber( phoneType.Id, "", newNumber, isMessagingEnabled, false, rockContext );
                    }

                    break;
                }
                case "address":
                case "city":
                case "state":
                case "zip":
                case "country":
                {
                    // we dont update the address using the information passed in the webhook event
                    // instead we query HubSpot api to get full address and then update Rock if needed
                    await UpdatePersonAddress( person, hubSpotContact.HubSpotObjectId, rockContext );

                    break;
                }
                default:
                    break;
            }

            rockContext.SaveChanges();
        }

        /// <summary>
        /// Update person attribute value in Rock
        /// </summary>
        /// <param name="hubSpotContact"></param>
        /// <param name="attributeId"></param>
        /// <param name="attributeFieldType"></param>
        /// <param name="propertyValue"></param>
        private static void UpdatePersonAttributeValueInRock( HubSpotContact hubSpotContact, int attributeId, string attributeFieldType, string propertyValue )
        {
            RockContext rockContext = new RockContext();
            AttributeValueService attributeValueService = new AttributeValueService( rockContext );
            AttributeValue attributeValue = attributeValueService.GetByAttributeIdAndEntityId( attributeId, hubSpotContact.PersonAlias.Person.Id );

            if ( attributeValue == null )
            {
                // attribute value doesnt exist, create a new one.
                attributeValue = new AttributeValue
                {
                    AttributeId = attributeId,
                    EntityId = hubSpotContact.PersonAlias.Person.Id
                };
                attributeValueService.Add( attributeValue );
            }

            if ( attributeFieldType == "boolean" )
            {
                attributeValue.Value = propertyValue == "true" ? "True" : "False" ;
            }
            else
            {
                // default action update value as is
                attributeValue.Value = propertyValue;
            }

            rockContext.SaveChanges();
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
            Location newAddressLocation = await RequestContactAddressFromHubSpot( rockContext, hubSpotObjectId );
            if ( newAddressLocation == null )
            {
                // unable to get valid Location, skip update
                throw new Exception( "Unable to get valid location" );
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
                if ( int.TryParse( birthDateValues[0], out int birthMonth ) )
                {
                    person.BirthMonth = birthMonth;
                }
                // day
                if ( int.TryParse( birthDateValues[1], out int birthDay ) )
                {
                    person.BirthDay = birthDay;
                }
            }

            if ( birthDateValues.Length == 3 && birthDateValues[2].Length == 4 )
            {
                // year
                if ( int.TryParse( birthDateValues[2], out int birthYear ) )
                {
                    person.BirthYear = birthYear;
                }
            }            
        }
#endregion

#region HubSpot API Call Methods

        /// <summary>
        /// Request contact info from HubSpot
        /// </summary>
        /// <param name="hubSpotObjectId"></param>
        /// <param name="contactProperties"></param>
        /// <returns></returns>
        private static async Task<HttpResponseMessage> RequestContactFromHubSpot( int hubSpotObjectId, List<string> contactProperties )
        {
            HttpResponseMessage hubSpotResponse = new HttpResponseMessage();

            // hubspot api configuration
            string hubSpot_APIUrl = GlobalAttributesCache.Value( "HubSpotAPIUrl" );
            string hubSpot_APIKey = GlobalAttributesCache.Value( "HubSpotAPIKey" );
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
        private static async Task<Location> RequestContactAddressFromHubSpot( RockContext rockContext, int hubSpotObjectId )
        {
            List<string> contactProperties = new List<string>
            {
                "address",
                "city",
                "state",
                "zip",
                "country"
            };            
            HttpResponseMessage hubSpotApiResponse = await RequestContactFromHubSpot( hubSpotObjectId, contactProperties );
            string responseString = await hubSpotApiResponse.Content.ReadAsStringAsync();
            if ( hubSpotApiResponse.StatusCode != HttpStatusCode.OK || responseString.IsNullOrWhiteSpace() )
            {
                // fail - api call failed or response empty
                throw new Exception( "RequestContact: HubSpot API Error" );
            }

            // HubSpot response is not guarenteed to have all the requested properties in its response.
            // Like the attribute value table, a HubSpot property does not get created for a contact
            // until a value is entered. We need to check for the keys in the response before we try
            // to assign their values to avoid null exceptions.
            string address = string.Empty;
            string city = string.Empty;
            string state = string.Empty;
            string zip = string.Empty;
            string country = string.Empty;

            // parse the response string into JObject
            JObject response = JObject.Parse( responseString );
            JObject responseProperties = ( JObject ) response["properties"];

            if ( JsonContainsKey( responseProperties, "address" ) )
            {
                address = ( string ) responseProperties["address"]["value"];
            }
            if ( JsonContainsKey( responseProperties, "city" ) )
            {
                city = ( string ) responseProperties["city"]["value"];
            }
            if ( JsonContainsKey( responseProperties, "state" ) )
            {
                state = ( string ) responseProperties["state"]["value"];
            }
            if ( JsonContainsKey( responseProperties, "zip" ) )
            {
                zip = ( string ) responseProperties["zip"]["value"];
            }
            if ( JsonContainsKey( responseProperties, "country" ) )
            {
                country = ( string ) responseProperties["country"]["value"];
            }

            if ( address.IsNullOrWhiteSpace() || city.IsNullOrWhiteSpace() || state.IsNullOrWhiteSpace() || zip.IsNullOrWhiteSpace() || country.IsNullOrWhiteSpace() )
            {
                // missing required info to get new location
                return null;
            }

            return new LocationService( rockContext )
                .Get( address, "", city, state, zip, country, true );;
        }

        /// <summary>
        /// Update contact properties in HubSpot
        /// </summary>
        /// <param name="hubSpotObjectId"></param>
        /// <param name="propertiesToUpdate"></param>
        /// <returns></returns>
        private static async Task<HttpResponseMessage> UpdateContactInHubSpot( int hubSpotObjectId, Dictionary<string, string> propertiesToUpdate )
        {
            HttpResponseMessage hubSpotResponse = new HttpResponseMessage();

            // hubspot api configuration
            string hubSpot_APIUrl = GlobalAttributesCache.Value( "HubSpotAPIUrl" );
            string hubSpot_APIKey = GlobalAttributesCache.Value( "HubSpotAPIKey" );
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

            // make the api request
            try
            {
                HttpClient client = new HttpClient();
                hubSpotResponse = await client.PostAsync( requestUrl.ToString(), new StringContent( requestBody.ToString(), Encoding.UTF8, "application/json" ) );
            }
            catch ( Exception e )
            {
                Debug.WriteLine( e.Message );
            }

            return hubSpotResponse;
        }

#endregion

#region Helper Methods

        /// <summary>
        /// Check if Json object contains specified key
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static bool JsonContainsKey( JObject jsonObject, string key )
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
    }
}
