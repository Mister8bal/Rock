using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.Entity;
using System.Linq;
using church.ccv.CCVCore.HubSpot.Model;
using church.ccv.CCVCore.HubSpot.Transactions;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;

namespace church.ccv.CCVCore.HubSpot.Workflow.Action
{
    /// <summary>
    /// Update contact in HubSpot
    /// </summary>
    [ActionCategory( "Utility" )]
    [Description( "Queues a transaction that will sync person changes to a contact in HubSpot" )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Update Contact In HubSpot" )]
    [WorkflowAttribute("Person", "Workflow attribute that contains the person to update.", true, "", "", 0, null,
        new string[] { "Rock.Field.Types.PersonFieldType" } )]
    [IntegerField( "Sync Differential", "The amount of time that needs to pass before syncing a field to HubSpot", true, 60, "", 1, "SyncDifferential" )]

    public class UpdateContactInHubSpot : ActionComponent
    {
        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            // prep for the action
            errorMessages = new List<string>();

            // get the person attribute value
            Person person = GetPersonFromActionAttribute("Person", rockContext, action, errorMessages);

            if ( person == null )
            {
                // no person, nothing do
                return true;
            }

            // look for a HubSpotContact for the person
            HubSpotContact hubSpotContact = new Service<HubSpotContact>( rockContext ).Queryable().Where( a => a.PersonAliasId == person.PrimaryAliasId ).FirstOrDefault();

            if ( hubSpotContact == null )
            {
                // we dont have a hubspot contact for this person - nothing to do
                return true;
            }

            // get the sync differential attribute value
            string syncDifferential = GetAttributeValue( action, "SyncDifferential" );
            int syncDifferentialValue = 0;
            int.TryParse( syncDifferential, out syncDifferentialValue );
            
            // we have a valid hubSpotContact, queue transaction to sync contact info
            var transaction = new SyncPersonToHubSpotTransaction
            {
                HubSpotObjectId = hubSpotContact.HubSpotObjectId,
                SyncDifferential = syncDifferentialValue
            };

            Rock.Transactions.RockQueue.TransactionQueue.Enqueue( transaction );

            return true;
        }

        /// <summary>
        /// Get person from attribute of workflow action
        /// </summary>
        /// <param name="key"></param>
        /// <param name="rockContext"></param>
        /// <param name="action"></param>
        /// <param name="errorMessages"></param>
        /// <returns></returns>
        private Person GetPersonFromActionAttribute(string key, RockContext rockContext, WorkflowAction action, List<string> errorMessages)
        {
            string value = GetAttributeValue( action, key );
            Guid guidPersonAttribute = value.AsGuid();
            if (!guidPersonAttribute.IsEmpty())
            {
                var attributePerson = AttributeCache.Read( guidPersonAttribute, rockContext );
                if (attributePerson != null)
                {
                    string attributePersonValue = action.GetWorklowAttributeValue(guidPersonAttribute);
                    if (!string.IsNullOrWhiteSpace(attributePersonValue))
                    {
                        if (attributePerson.FieldType.Class == "Rock.Field.Types.PersonFieldType")
                        {
                            Guid personAliasGuid = attributePersonValue.AsGuid();
                            if (!personAliasGuid.IsEmpty())
                            {
                                PersonAliasService personAliasService = new PersonAliasService(rockContext);
                                return personAliasService.Queryable().AsNoTracking()
                                    .Where(a => a.Guid.Equals(personAliasGuid))
                                    .Select(a => a.Person)
                                    .FirstOrDefault();
                            }
                            else
                            {
                                errorMessages.Add(string.Format("Person could not be found for selected value ('{0}')!", guidPersonAttribute.ToString()));
                                return null;
                            }
                        }
                        else
                        {
                            errorMessages.Add(string.Format("The attribute used for {0} to provide the person was not of type 'Person'.", key));
                            return null;
                        }
                    }
                }
            }

            return null;
        }
    }
}
