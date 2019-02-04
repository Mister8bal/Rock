using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
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
    [Description( "Uses the specified Registration Instance Guid to post an order to ShipRush through their API.  The registration must have a T-ShirtSize attribute key for this to function properly." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "ShipRush API Post" )]
    [TextField( "Api Url", "The api url", true, "", "", 1, "ApiUrl" )]
    [TextField( "Registration Guid", "The registration Guid to generate the ShipRush order from <span class='tip tip-lava'></span>", true, "", "", 2, "RegistrationGuid" )]
    [WorkflowAttribute( "Post Response", "Attribute to assign the post response", true, "", "", 3, "PostResponse" )]
    [WorkflowAttribute( "Post Status", "Attribute to assign the status of the post", true, "", "", 4, "PostStatus")]
    public class PostToShipRush : ActionComponent
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

            // Get the API Url / ensure its not empty
            string apiUrl = GetAttributeValue( action, "ApiUrl" );

            if ( apiUrl.IsNullOrWhiteSpace() )
            {
                SetResponseAttributes( rockContext, action, false, "Missing API Url" );

                return true;
            }

            // Get the registration guid / ensure its not empty
            string registrationGuid = GetAttributeValue( action, "RegistrationGuid" );

            registrationGuid = registrationGuid.ResolveMergeFields( mergeFields );

            if ( registrationGuid.IsNullOrWhiteSpace() )
            {
                SetResponseAttributes( rockContext, action, false, "Missing Registration Guid" );

                return true;
            }

            // load the registration / ensure its not empty
            Registration registration = new RegistrationService( rockContext ).Get( registrationGuid.AsGuid() );

            if ( registration == null)
            {
                SetResponseAttributes( rockContext, action, false, "Registration not found" );

                return true;
            }

            // load the registrant / ensure its not empty
            RegistrationRegistrant registrant = registration.Registrants.FirstOrDefault();

            if ( registrant == null )
            {
                SetResponseAttributes( rockContext, action, false, "Missing Registrant" );

                return true;
            }

            // load the home address / ensure its not empty
            Location homeAddress = new Location();

            Group familyGroup = registrant.Person.GetFamily();

            Guid? homeAddressGuid = Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuidOrNull();
            if ( homeAddressGuid.HasValue )
            {
                DefinedValueCache homeAddressDv = DefinedValueCache.Read( homeAddressGuid.Value );
                if ( homeAddressDv != null )
                {
                    GroupLocation homeLocation = familyGroup.GroupLocations.Where( a => a.GroupLocationTypeValueId == homeAddressDv.Id ).FirstOrDefault();

                    if ( homeLocation != null )
                    {
                        homeAddress = homeLocation.Location;
                    }
                }
            }
            
            // ensure home address is not empty
            if ( homeAddress == null )
            {
                SetResponseAttributes( rockContext, action, false, "Missing registrant home address" );

                return true;
            }

            // build ShipRushOrder XML from registration object
            string requestOrder = CreateShipRushOrderXml( registration, registrant, homeAddress );

            // make API call
            try
            {
                // set up REST request
                var client = new RestClient( apiUrl );
                var request = new RestRequest( Method.POST );

                request.AddParameter( "application/xml", requestOrder, ParameterType.RequestBody );

                // execute response
                IRestResponse response = client.Execute( request );

                // ensure Ok response
                if ( response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    SetResponseAttributes( rockContext, action, false, response.ErrorMessage );

                    return true;
                }

                SetResponseAttributes( rockContext, action, true, response.Content );

                return true;
            }
            catch ( Exception ex )
            {
                // something failed
                SetResponseAttributes( rockContext, action, false, ex.Message );

                return true;
            }
        }

        /// <summary>
        /// Set the response attributes of the workflow
        /// </summary>
        /// <param name="rockContext"></param>
        /// <param name="action"></param>
        /// <param name="responseStatus"></param>
        /// <param name="responseMessage"></param>
        private void SetResponseAttributes( RockContext rockContext, WorkflowAction action, bool responseStatus, string responseMessage )
        {
            // get the attributes to write to
            var statusAttribute = AttributeCache.Read( GetAttributeValue( action, "PostStatus" ).AsGuid(), rockContext );
            var responseAttribute = AttributeCache.Read( GetAttributeValue( action, "PostResponse" ).AsGuid(), rockContext );

            // Set the status attribute
            SetWorkflowAttributeValue( action, statusAttribute.Guid, responseStatus.ToString() );
            action.AddLogEntry( string.Format( "Set '{0}' attribute to '{1}'", statusAttribute.Name, responseStatus.ToString() ), true );

            // Set the response attribute
            SetWorkflowAttributeValue( action, responseAttribute.Guid, responseMessage );
            action.AddLogEntry( string.Format( "Set '{0}' attribute to '{1}'", responseAttribute.Name, responseMessage ), true );
        }

        /// <summary>
        /// Build a ShipRushOrder object
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="registrant"></param>
        /// <param name="homeAddress"></param>
        /// <returns></returns>
        private string CreateShipRushOrderXml( Registration  registration, RegistrationRegistrant registrant, Location homeAddress )
        {
            ShipRushOrder shipRushOrder = new ShipRushOrder();

            // hydrate the registrant attributes
            registrant.LoadAttributes();

            // order info
            shipRushOrder.OrderNumber = registration.Id.ToString();
            shipRushOrder.TShirtSize = registrant.AttributeValues.Where( a => a.Key == "T-ShirtSize" ).Select( v => v.Value.Value ).FirstOrDefault();

            // person info
            shipRushOrder.FirstName = registrant.FirstName;
            shipRushOrder.LastName = registrant.LastName;         

            // home address
            shipRushOrder.Address1 = homeAddress.Street1;
            shipRushOrder.Address2 = homeAddress.Street2;
            shipRushOrder.City = homeAddress.City;
            shipRushOrder.State = homeAddress.State;
            shipRushOrder.PostalCode = homeAddress.PostalCode;
            shipRushOrder.Country = homeAddress.Country;

            // home phone number
            var homePhone = registrant.Person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() );
            shipRushOrder.Phone = homePhone.NumberFormatted;

            return CreateShipRushXML( shipRushOrder );
        }

        /// <summary>
        /// Create Shiprush XML
        /// </summary>
        /// <param name="shipRushOrder"></param>
        /// <returns></returns>
        private string CreateShipRushXML( ShipRushOrder shipRushOrder )
        {
            StringBuilder builder = new StringBuilder();

            using ( var stringWriter = new StringWriterWithEncoding( builder, Encoding.UTF8 ) )
            {
                using ( var xmlWriter = XmlWriter.Create( stringWriter ) )
                {
                    xmlWriter.WriteStartDocument();
                        xmlWriter.WriteStartElement( "Request" );
                            xmlWriter.WriteStartElement( "ShipTransaction" );
                                xmlWriter.WriteStartElement( "Order" );
                                    xmlWriter.WriteElementString( "OrderNumber", shipRushOrder.OrderNumber );
                                    xmlWriter.WriteElementString( "PaymentStatus", "2" );
                                    xmlWriter.WriteElementString( "ShipmentType", "Pending" );
                                    xmlWriter.WriteStartElement( "ShipmentOrderItem" );
                                        xmlWriter.WriteElementString( "Name", shipRushOrder.TShirtSize );
                                        xmlWriter.WriteElementString( "Quantity", "1" );
                                    xmlWriter.WriteEndElement();
                                xmlWriter.WriteEndElement();
                                xmlWriter.WriteStartElement( "Shipment" );
                                    xmlWriter.WriteStartElement( "Package" );
                                        xmlWriter.WriteElementString( "PackageReference1", shipRushOrder.OrderNumber );
                                    xmlWriter.WriteEndElement();
                                    xmlWriter.WriteStartElement( "DeliveryAddress" );
                                        xmlWriter.WriteStartElement( "Address" );
                                            xmlWriter.WriteElementString( "FirstName", shipRushOrder.FirstName + " " + shipRushOrder.LastName );
                                            xmlWriter.WriteElementString( "Address1", shipRushOrder.Address1 );
                                            xmlWriter.WriteElementString( "Address2", shipRushOrder.Address2 );
                                            xmlWriter.WriteElementString( "City", shipRushOrder.City );
                                            xmlWriter.WriteElementString( "State", shipRushOrder.State );
                                            xmlWriter.WriteElementString( "PostalCode", shipRushOrder.PostalCode );
                                            xmlWriter.WriteElementString( "Country", shipRushOrder.Country );
                                            xmlWriter.WriteElementString( "Phone", shipRushOrder.Phone );
                                        xmlWriter.WriteEndElement();
                                    xmlWriter.WriteEndElement();
                                xmlWriter.WriteEndElement();
                            xmlWriter.WriteEndElement();
                        xmlWriter.WriteEndElement();
                    xmlWriter.WriteEndDocument();
                }

                return stringWriter.ToString();
            }            
        }

        /// <summary>
        /// String Writer class that overrides the Encoding property
        /// </summary>
        public class StringWriterWithEncoding : StringWriter
        {
            private readonly Encoding _encoding;

            public StringWriterWithEncoding( StringBuilder sb, Encoding encoding) : base( sb )
            {
                this._encoding = encoding;
            }

            public override Encoding Encoding
            {
                get
                {
                    return this._encoding;
                }
            }
        }

        /// <summary>
        /// ShipRush Order Object
        /// </summary>
        public class ShipRushOrder
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
