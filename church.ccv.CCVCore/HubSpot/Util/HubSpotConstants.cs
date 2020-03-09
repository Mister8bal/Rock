namespace church.ccv.CCVCore.HubSpot.Util
{
    public class HubSpotConstants
    {
        /// <summary>
        /// Api Key names in Rock.
        /// </summary>
        public static class Api
        {
            public const string Url = "HubSpotAPIUrl";
            public const string Key = "HubSpotAPIKey";
        }

        /// <summary>
        /// Sync direction
        /// </summary>
        public static class SyncDirection
        {
            public const string ToHubSpot = "to";
            public const string FromHubSpot = "from";
        }

        /// <summary>
        /// Contact property key names in HubSpot.
        /// </summary>
        public static class ContactPropertyKey
        {
            public const string FirstName = "firstname";
            public const string LastName = "lastname";
            public const string Email = "email";
            public const string DateOfBirth = "date_of_birth";
            public const string Age = "age";
            public const string Phone = "phone";
            public const string MobilePhone = "mobilephone";
            public const string Address = "address";
            public const string City = "city";
            public const string State = "state";
            public const string Zip = "zip";
            public const string Country = "country";
            public const string EmailOptOut = "hs_email_optout";
            // HubSpot rock_id is matched to Rock PersonAliasId
            // rock_id was created by a non developer before the project started
            // the property can not be changed so it would be a lot of work 
            // resetting up the contacts with a new property sooo here we are
            public const string RockId = "rock_id";
            public const string ConnectionStatus = "connection_status";
            public const string MemberStatus = "member_status";
            public const string FamilyId = "family_id";
        }

        /// <summary>
        /// Valid value options for various properties in HubSpot
        /// </summary>
        public static class ContactPropertyValue
        {
            public const string ConnectionStatus_Member = "Member";
            public const string ConnectionStatus_Attendee = "Attendee";
            public const string ConnectionStatus_StarsParticipant = "Stars Participant";
            public const string ConnectionStatus_Visitor = "Visitor";
            public const string ConnectionStatus_Participant = "Participant";
            public const string ConnectionStatus_WebProspect = "Web Prospect";
            public const string ConnectionStatus_VisitorProspect = "Visitor Prospect";
            public const string ConnectionStatus_FormerMember = "Former Member";
            public const string ConnectionStatus_Missionary = "Missionary";
            public const string ConnectionStatus_Corporate = "Corporate";
            public const string MemberStatus_Active = "Active";
            public const string MemberStatus_Inactive = "Inactive";
        }

        /// <summary>
        /// Attribute Key names for the HubSpotPropertyMap Defined Type in Rock.
        /// </summary>
        public static class ContactPropertyMapKey
        {
            public const string RockAttribute = "RockAttribute";
            public const string ExcludeStaff = "ExcludeStaff";
        }

        /// <summary>
        /// Web hook event key names in HubSpot.
        /// </summary>
        public static class WebHookEventKey
        {
            public const string ObjectId = "objectId";
            public const string MergedVids = "merged-vids";
            public const string SubscriptionType = "subscriptionType";
            public const string Properties = "properties";
            public const string PropertyName = "propertyName";
            public const string PropertyValue = "propertyValue";
        }

        /// <summary>
        /// Rock history field names for person fields and attributes.
        /// Standardize to have no spaces or special characters and all lowercase.
        /// </summary>
        public static class RockHistoryFieldName
        {
            public const string FirstName = "First Name";
            public const string NickName = "Nick Name";
            public const string LastName = "Last Name";
            public const string Email = "Email";
            public const string EmailPreference = "Email Preference";
            public const string Phone = "Main/Home Phone";
            public const string MobilePhone = "Mobile Phone";
            public const string BirthDate = "Birth Date";
            public const string Age = "Age";
            public const string ConnectionStatus = "Connection Status";
            public const string RecordSatus = "Record Status";
            // address history is currently not supported in our version of Rock
            // stubbing out what will be needed for when this is implemented
            public const string Street = "Street";
            public const string City = "City";
            public const string State = "State";
            public const string ZipCode = "Zip Code";
            public const string Country = "Country";
        }
    }
}
