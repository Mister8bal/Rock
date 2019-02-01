using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using RestSharp;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;

namespace church.ccv.Utility.Workflow.Action
{
    /// <summary>
    /// Post to Shiprush API to add an order.
    /// </summary>
    [ActionCategory( "Utility" )]
    [Description( "Uses the specified Registration Instance Id to post an order to ShipRush" )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "ShipRush API Post" )]
    [TextField( "Api Url", "The URL to use</span>", false, "", "", 1, "ApiUrl" )]
    [TextField( "Registration Guid", "The registration Guid to generate the ShipRush order from <span class='tip tip-lava'></span>", false, "", "", 2, "RegistrationGuid" )]
    class PostToShipRush : ActionComponent
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
            var mergeFields = GetMergeFields( action );

            // Get and resolve merge feilds for the registration instance guid
            var registrationGuid = GetAttributeValue( action, "RegistrationGuid" );
            registrationGuid = registrationGuid.ResolveMergeFields( mergeFields );

            // ensure registration guid is not empty before proceeding
            if ( registrationGuid.IsNullOrWhiteSpace() )
            {
                action.AddLogEntry( "Missing Registration Guid", true );
                errorMessages.Add( "Missing Registration Guid" );

                return false;
            }

            // load the registration
            var registration = new RegistrationService( rockContext ).Get( registrationGuid.AsGuid() );

            // ensure registration is not empty before proceeding
            if ( registration == null)
            {
                action.AddLogEntry( "Registraion not found", true );
                errorMessages.Add( "Registration not found" );

                return false;
            }

            var registrant = registration.Registrants.FirstOrDefault();

            // build ShipRushOrder from registration object
            ShipRushOrder requestOrder = CreateShipRushOrder( registration, registrant );


            // make API call
            try
            {
                // convert object to xml
                var requestBody = ConvertToXML( requestOrder );


                // set up REST request
                // var client = new RestClient( GetAttributeValue( action, "ApiUrl" ) );
                //var request = new RestRequest( Method.POST );






                return true;
            }
            catch ( Exception ex )
            {
                action.AddLogEntry( ex.Message, true );
                errorMessages.Add( ex.Message );

                return false;
            }
        }

        private ShipRushOrder CreateShipRushOrder( Registration  registration, RegistrationRegistrant registrant )
        {
            ShipRushOrder shipRushOrder = new ShipRushOrder();

            registrant.LoadAttributes();

            // order info
            shipRushOrder.OrderNumber = registration.Id.ToString();
            shipRushOrder.TShirtSize = registrant.AttributeValues.Where( a => a.Key == "T-ShirtSize" ).Select( v => v.Value.Value ).FirstOrDefault();

            // person info
            shipRushOrder.FirstName = registrant.FirstName;
            shipRushOrder.LastName = registrant.LastName;                 



            // mailing address

            // use address key on registration

            var mailingAddress = registrant.Person.GetHomeLocation();

            //shipRushOrder.Address1 = mailingAddress.Street1;
            //shipRushOrder.Address2 = mailingAddress.Street2;
            //shipRushOrder.City = mailingAddress.City;
            //shipRushOrder.State = mailingAddress.State;
            //shipRushOrder.PostalCode = mailingAddress.PostalCode;
            //shipRushOrder.Country = mailingAddress.Country;

            // phone number

            // use Home Phone key on registration

            //var phone = registrant.Person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() );

            //shipRushOrder.Phone = phone.NumberFormatted;
                                 
            return shipRushOrder;
        }

        /// <summary>
        /// Conver object to XML
        /// </summary>
        /// <param name="requestOrder"></param>
        /// <returns></returns>
        private string ConvertToXML( ShipRushOrder shipRushOrder )
        {
            using ( var stringWriter = new StringWriter() )
            {
                var serializer = new XmlSerializer( shipRushOrder.GetType() );
                serializer.Serialize( stringWriter, shipRushOrder );

                return stringWriter.ToString();
            }
        }

        protected class ShipRushOrder
        {
            public string OrderNumber { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Address1 { get; set; }
            public string Address2 { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string Country { get; set; }
            public string PostalCode { get; set; }
            public string Phone { get; set; }
            public string TShirtSize { get; set; }

            public ShipRushOrder()
            {
            }
        }
    }
}
