using System;
using System.Collections.Generic;
using System.Linq;
using church.ccv.CCVCore.HubSpot.Model;
using church.ccv.CCVCore.HubSpot.Util;
using Rock;
using Rock.Data;
using Rock.Transactions;
using static church.ccv.CCVCore.HubSpot.Util.HubSpotApi;

namespace church.ccv.CCVCore.HubSpot.Transactions
{
    /// <summary>
    /// Sync's a person's to a contact in HubSpot
    /// </summary>
    class SyncPersonToHubSpotTransaction : ITransaction
    {
        public int HubSpotObjectId { get; set; }

        public int SyncDifferential { get; set; }

        private readonly DateTime SyncTimestamp = DateTime.Now;

        /// <summary>
        /// Execute method
        /// </summary>
        public async void Execute()
        {
            // ensure we have what we need to update
            if ( HubSpotObjectId <= 0 )
            {
                // missing required info, cancel update
                return;
            }

            RockContext rockContext = new RockContext();

            // get the hubSpotContact
            HubSpotContact hubSpotContact = new Service<HubSpotContact>( rockContext ).Queryable().Where( a => a.HubSpotObjectId == HubSpotObjectId ).FirstOrDefault();
            if ( hubSpotContact == null )
            {
                // no contact found, cancel update
                return;
            }

            // get all history changes since the last sync and remove any that were recently synced by the HubSpot webhook
            List<string> rockFieldsThatChanged = HubSpotHistoryService.GetFieldsFromHistory( hubSpotContact.PersonAlias.Person, 
                                                                                             hubSpotContact.LastSyncDateTime, 
                                                                                             Rock.SystemGuid.Category.HISTORY_PERSON_DEMOGRAPHIC_CHANGES.AsGuid() );

            List<string> rockFieldsMinusWebhookUpdates = HubSpotHistoryService.RemoveFieldsUpdatedByHubSpotWebhook( hubSpotContact.PersonAlias.Person,
                                                                                                                    rockFieldsThatChanged,
                                                                                                                    SyncDifferential );
            if ( rockFieldsMinusWebhookUpdates.Count == 0 )
            {
                // no fields to sync, cancel update
                return;
            }
            
            // sync fields with HubSpot   
            ApiResult syncResult = await SyncContactToHubSpot( hubSpotContact, rockFieldsMinusWebhookUpdates );

            // if sync was success, update the LastSyncDateTime
            if ( syncResult == ApiResult.Success )
            {
                hubSpotContact.LastSyncDateTime = SyncTimestamp;

                rockContext.SaveChanges();
            }
        }
    }
}
