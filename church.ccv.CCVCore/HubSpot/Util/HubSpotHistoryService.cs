using System;
using System.Collections.Generic;
using System.Linq;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using static church.ccv.CCVCore.HubSpot.Util.HubSpotConstants;

namespace church.ccv.CCVCore.HubSpot.Util
{
    public class HubSpotHistoryService
    {
        /// <summary>
        /// Get fields from a persons history
        /// </summary>
        /// <param name="person"></param>
        /// <param name="fromDateTime"></param>
        /// <param name="category"></param>
        /// <param name="hubSpotSyncDirection"></param>
        /// <returns></returns>
        public static List<string> GetFieldsFromHistory( Person person, DateTime fromDateTime, Guid category, string hubSpotSyncDirection = "" )
        {
            // return object
            List<string> fieldsFound = new List<string>();

            // get history entries
            int personEntityTypeId = EntityTypeCache.Read( typeof( Person ) ).Id;
            int groupEntityTypeId = EntityTypeCache.Read( typeof( Group ) ).Id;
            List<int> familyIds = person.GetFamilies().Select( a => a.Id).ToList();

            List<string> historyEntries = new HistoryService( new RockContext() ).Queryable( "CreatedByPersonAlias.Person" )
                        .Where( a =>
                              // filter by category
                              ( a.Category.Guid == category ) &&
                              // we only want person or family history
                              ( ( a.EntityTypeId == personEntityTypeId && a.EntityId == person.Id ) ||
                                ( a.EntityTypeId == groupEntityTypeId && familyIds.Contains( a.EntityId ) ) ) &&
                              // filter items from date time specified
                              ( a.CreatedDateTime > fromDateTime || a.ModifiedDateTime > fromDateTime ) &&
                              // finally we only want items that are field updates
                              ( a.Summary.Contains( "<span class='field-name'>" ) ) && 
                              ( a.Summary.Contains( "Added" ) || a.Summary.Contains( "Modified" ) || a.Summary.Contains( "Synced" ) ) )
                        .Select( b => b.Summary )
                        .ToList();

            foreach ( string entry in historyEntries )
            {
                if ( entry.Contains("span") )
                {
                    // if syncDirection is not specified, include everything
                    // otherwise include the item if it matches the direction specified
                    bool include = false;
                    if ( hubSpotSyncDirection == "" )
                    {
                        include = true;
                    }
                    else if ( hubSpotSyncDirection == SyncDirection.ToHubSpot && entry.Contains( "to HubSpot") )
                    {
                        include = true;
                    }
                    else if ( hubSpotSyncDirection == SyncDirection.FromHubSpot && entry.Contains( "from HubSpot" ) )
                    {
                        include = true;
                    }

                    if ( include == true )
                    {
                        // parse the field name
                        string fieldName = GetContentFromSpan( entry, "field-name" );
                        // add item to fieldsUpdated if its not already in there
                        if ( !fieldsFound.Contains( fieldName ) )
                        {
                            fieldsFound.Add( fieldName );
                        }
                    }                                     
                }
            }

            // if date of birth changed, also update age
            if ( fieldsFound.Contains( RockHistoryFieldName.BirthDate ) )
            {
                fieldsFound.Add( RockHistoryFieldName.Age );
            }

            return fieldsFound;
        }

        /// <summary>
        /// Remove history fields that have been recently updated by HubSpot Webhook
        /// </summary>
        /// <param name="person"></param>
        /// <param name="fields"></param>
        /// <param name="syncDifferential"></param>
        /// <returns></returns>
        public static List<string> RemoveFieldsUpdatedByHubSpotWebhook( Person person, List<string> fields, int syncDifferential )
        {
            List<string> returnFields = fields;

            // set a default value for SyncDifferential if needed
            // note: number is negative
            syncDifferential = -Math.Abs( syncDifferential > 0 ? syncDifferential : 60 );

            // get the fields that have recently been updated with HubSpot since sync differential
            List<string> recentWebHookFields = GetFieldsFromHistory( person, 
                                                                     DateTime.Now.AddSeconds( syncDifferential ), 
                                                                     church.ccv.Utility.SystemGuids.Category.HISTORY_PERSON_HUBSPOT.AsGuid(), 
                                                                     SyncDirection.FromHubSpot );

            // loop through the recent webhook updates and remove from returnFields
            foreach ( string field in recentWebHookFields )
            {
                switch ( field )
                {
                    case ContactPropertyKey.FirstName:
                        returnFields.Remove( RockHistoryFieldName.FirstName );
                        returnFields.Remove( RockHistoryFieldName.NickName );
                        break;
                    case ContactPropertyKey.LastName:
                        returnFields.Remove( RockHistoryFieldName.LastName );
                        break;
                    case ContactPropertyKey.Email:
                        returnFields.Remove( RockHistoryFieldName.Email );
                        break;
                    case ContactPropertyKey.Phone:
                        returnFields.Remove( RockHistoryFieldName.Phone );
                        break;
                    case ContactPropertyKey.MobilePhone:
                        returnFields.Remove( RockHistoryFieldName.MobilePhone );
                        break;
                    case ContactPropertyKey.Address:
                    case ContactPropertyKey.City:
                    case ContactPropertyKey.State:
                    case ContactPropertyKey.Zip:
                    case ContactPropertyKey.Country:
                        // address syncing on hold until
                        // our current version of rock does not support group history
                        // we are evaluating whether to add support to our version
                        // or hold this until after the upgrade
                        break;
                    case ContactPropertyKey.DateOfBirth:
                        returnFields.Remove( RockHistoryFieldName.BirthDate );
                        break;
                    case ContactPropertyKey.Age:
                        returnFields.Remove( RockHistoryFieldName.Age );
                        break;
                    case ContactPropertyKey.ConnectionStatus:
                        returnFields.Remove( RockHistoryFieldName.ConnectionStatus );
                        break;
                    case ContactPropertyKey.MemberStatus:
                        returnFields.Remove( RockHistoryFieldName.RecordSatus );
                        break;
                    default:
                        break;
                }
            }

            return returnFields;
        }

        /// <summary>
        /// Get the content of the first <span> found
        /// </summary>
        /// <param name="text"></param>
        /// <param name="cssClass"></param>
        /// <returns></returns>
        private static string GetContentFromSpan( string text, string cssClass )
        {
            // dont proceed if text does not have a span or if the text does not contain the cssClass requested
            if ( !text.Contains( "span" ) || !text.Contains( cssClass ) )
            {
                return string.Empty;
            }

            string foundContent = string.Empty;

            // first get index of closing bracket of first span with requested css class
            string searchPattern = string.Format( "<span class='{0}'>", cssClass );
            int beginCutIndex = text.IndexOf( searchPattern ) + searchPattern.Length;
            if ( beginCutIndex >= 0 )
            {
                // cut string starting at beginCutIndex
                foundContent = text.Substring( beginCutIndex );

                // cut off remainder of string starting at closest closing span
                int endCutIndex = foundContent.IndexOf( "</span>" );
                if ( endCutIndex >= 0 )
                {
                    // cut everything after closing span
                    foundContent = foundContent.Substring( 0, endCutIndex );
                }
            }

            return foundContent;
        }

        /// <summary>
        /// Add single change entry to Person history
        /// </summary>
        /// <param name="changeEntry"></param>
        /// <param name="personId"></param>
        public static void AddRockPersonHistory( string changeEntry, int personId )
        {
            List<string> changes = new List<string>()
            {
                changeEntry
            };
            SavePersonHistoryChanges( Rock.SystemGuid.Category.HISTORY_PERSON_DEMOGRAPHIC_CHANGES.AsGuid(), personId, changes );
        }

        /// <summary>
        /// Add single change entry to Person HubSpot history
        /// </summary>
        /// <param name="changeEntry"></param>
        /// <param name="personId"></param>
        public static void AddHubSpotHistory( string changeEntry, int personId )
        {
            List<string> changes = new List<string>()
            {
                changeEntry
            };
            SavePersonHistoryChanges( church.ccv.Utility.SystemGuids.Category.HISTORY_PERSON_HUBSPOT.AsGuid(), personId, changes );
        }

        /// <summary>
        /// Add change entries to Person HubSpot history
        /// </summary>
        /// <param name="changes"></param>
        /// <param name="personId"></param>
        public static void AddHubSpotHistory( List<string> changes, int personId )
        {
            SavePersonHistoryChanges( church.ccv.Utility.SystemGuids.Category.HISTORY_PERSON_HUBSPOT.AsGuid(), personId, changes );
        }

        /// <summary>
        /// Add HubSpot Person history entry
        /// </summary>
        /// <param name="syncDirection"></param>
        /// <param name="personId"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        public static void AddHubSpotHistory( string syncDirection, int personId, string propertyName, string propertyValue )
        {
            List<string> changes = new List<string>()
            {
                CreateChangeEntry( syncDirection, propertyName, propertyValue )
            };
            SavePersonHistoryChanges( church.ccv.Utility.SystemGuids.Category.HISTORY_PERSON_HUBSPOT.AsGuid(), personId, changes );
        }

        /// <summary>
        /// Add HubSpot Person history entries
        /// </summary>
        /// <param name="syncDirection"></param>
        /// <param name="personId"></param>
        /// <param name="propertiesUpdated"></param>
        public static void AddHubSpotHistory( string syncDirection, int personId, Dictionary<string, string> propertiesUpdated )
        {
            List<string> changes = new List<string>();
            foreach ( var propertyUpdated in propertiesUpdated )
            {
                changes.Add( CreateChangeEntry( syncDirection, propertyUpdated.Key, propertyUpdated.Value ) );
            }
            SavePersonHistoryChanges( church.ccv.Utility.SystemGuids.Category.HISTORY_PERSON_HUBSPOT.AsGuid(), personId, changes );
        }

        /// <summary>
        /// Save Person history changes
        /// </summary>
        /// <param name="historyCategory"></param>
        /// <param name="personId"></param>
        /// <param name="changes"></param>
        private static void SavePersonHistoryChanges( Guid historyCategory, int personId, List<string> changes )
        {
            RockContext rockContext = new RockContext();

            HistoryService.AddChanges( rockContext, typeof( Person ), historyCategory, personId, changes );

            rockContext.SaveChanges();
        }

        /// <summary>
        /// Create change entry for history
        /// </summary>
        /// <param name="syncDirection"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        public static string CreateChangeEntry( string syncDirection, string propertyName, string propertyValue )
        {
            return string.Format( "Synced <span class='field-name'>{0}</span> value <span class='field-value'>{1}</span> {2} HubSpot.", propertyName, propertyValue, syncDirection );
        }
    }
}
