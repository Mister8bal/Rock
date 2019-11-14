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
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;

using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Rock.Workflow.Action
{
    /// <summary>
    /// Sends email
    /// </summary>
    [ActionCategory( "Communications" )]
    [Description( "Sends a SMS message to a specific role within a group. The recipients will be restricted to a specific group role within a group." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "SMS Send To Group With Group Role" )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.COMMUNICATION_SMS_FROM, "From", "The number to originate message from (configured under Admin Tools > General Settings > Defined Types > SMS From Values).", true, false, "", "", 0 )]
    [WorkflowAttribute( "Recipients Group", "A Group attribute that contains the persons or phone numbers that messages should be sent to. <span class='tip tip-lava'></span>", true, "", "", 1, "To",
        new string[] { "Rock.Field.Types.GroupFieldType" } )]
    [WorkflowAttribute( "Send to Group Role", "A Group Role attribute to limit recipients.", true, "", "", 2, "GroupRole",
        new string[] { "Rock.Field.Types.GroupRoleFieldType" } )]
    [WorkflowTextOrAttribute( "Message", "Attribute Value", "The message or an attribute that contains the message that should be sent. <span class='tip tip-lava'></span>", true, "", "", 2, "Message",
        new string[] { "Rock.Field.Types.TextFieldType", "Rock.Field.Types.MemoFieldType" } )]
    [WorkflowAttribute( "Attachment", "Workflow attribute that contains the attachment to be added. Note that when sending attachments with MMS; jpg, gif, and png images are supported for all carriers. Support for other file types is dependent upon each carrier and device. So make sure to test sending this to different carriers and phone types to see if it will work as expected.", false, "", "", 3, null,
        new string[] { "Rock.Field.Types.FileFieldType", "Rock.Field.Types.ImageFieldType" } )]
    public class SendSmsToRole : ActionComponent
    {
        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute( RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            var mergeFields = GetMergeFields( action );

            // Get the From value
            int? fromId = null;
            Guid? fromGuid = GetAttributeValue( action, "From" ).AsGuidOrNull();
            if ( fromGuid.HasValue )
            {
                var fromValue = DefinedValueCache.Read( fromGuid.Value, rockContext );
                if ( fromValue != null )
                {
                    fromId = fromValue.Id;
                }
            }

            // Get the recipients
            var recipientsList = new List<RecipientData>();
            string toGroupAttribute = GetAttributeValue( action, "To" );
            Guid toGroupAttributeGuid = toGroupAttribute.AsGuid();

            if ( toGroupAttributeGuid.IsEmpty() )
            {
                action.AddLogEntry( "Invalid Submission: No valid group attribute guid", true );
                return false;
            }

            // Get the selected groups guid usig the Appropriate attribute guid.
            string toAttributeValue = action.GetWorklowAttributeValue( toGroupAttributeGuid );

            if ( string.IsNullOrWhiteSpace( toAttributeValue ) )
            {
                action.AddLogEntry( "Invalid Group: No valid group", true );
                return false;
            }
            
            Guid? groupGuid = toAttributeValue.AsGuidOrNull();

            //Get the Group Role attribute value
            Guid? groupRoleValueGuid = GetGroupRoleValue( action );

            IEnumerable<Person> groupRecipients = null;

            if ( groupGuid.HasValue && groupRoleValueGuid.HasValue )
            {
                groupRecipients = new GroupMemberService( rockContext ).GetByGroupGuid( groupGuid.Value )
                    .Where( m => m.GroupRole != null && m.GroupRole.Guid.Equals( groupRoleValueGuid.Value ) && m.GroupMemberStatus == GroupMemberStatus.Active )
                    .Select( m => m.Person );
            }
            else
            {
                action.AddLogEntry( "Invalid Recipient: No valid group Guid or group role Guid", true );
                return false;
            }


            if ( groupRecipients.Count() <= 0 )
            {
                return true;
            }

            foreach ( var person in groupRecipients)
            {
                var phoneNumber = person.PhoneNumbers
                    .Where( p => p.IsMessagingEnabled )
                    .FirstOrDefault();
                if ( phoneNumber != null )
                {
                    string smsNumber = phoneNumber.Number;
                    if ( !string.IsNullOrWhiteSpace( phoneNumber.CountryCode ) )
                    {
                        smsNumber = "+" + phoneNumber.CountryCode + phoneNumber.Number;
                    }

                    var recipientMergeFields = new Dictionary<string, object>( mergeFields );
                    var recipient = new RecipientData( smsNumber, recipientMergeFields );
                    recipientsList.Add( recipient );
                    recipient.MergeFields.Add( "Person", person );
                }
            }

            // Get the message
            string message = GetAttributeValue( action, "Message" );
            Guid? messageGuid = message.AsGuidOrNull();
            if ( messageGuid.HasValue )
            {
                var msgAttribute = AttributeCache.Read( messageGuid.Value, rockContext );
                if ( msgAttribute != null )
                {
                    string messageAttributeValue = action.GetWorklowAttributeValue( messageGuid.Value );
                    if ( !string.IsNullOrWhiteSpace( messageAttributeValue ) )
                    {
                        if ( msgAttribute.FieldType.Class == "Rock.Field.Types.TextFieldType" ||
                            msgAttribute.FieldType.Class == "Rock.Field.Types.MemoFieldType" )
                        {
                            message = messageAttributeValue;
                        }
                    }
                }
            }

            // Add the attachment (if one was specified)
            var binaryFile = new BinaryFileService( rockContext ).Get( GetAttributeValue( action, "Attachment", true ).AsGuid() );

            // Send the message
            if ( recipientsList.Any() && !string.IsNullOrWhiteSpace( message ) )
            {
                var smsMessage = new RockSMSMessage();
                smsMessage.SetRecipients( recipientsList );
                smsMessage.FromNumber = DefinedValueCache.Read( fromId.Value );
                smsMessage.Message = message;
                if ( binaryFile != null )
                {
                    smsMessage.Attachments.Add( binaryFile );
                }

                smsMessage.Send();
            }

            return true;
        }

        /// <summary>
        /// Returns the group role guid using the Workflow Action Object.
        /// </summary>
        /// <param name="action">The workflow action object.</param>
        /// <returns>A group role guid or null</returns>
        private Guid? GetGroupRoleValue( WorkflowAction action )
        {
            Guid? groupRoleGuid = null;
            Guid? groupRoleAttributeGuid = GetAttributeValue( action, "GroupRole" ).AsGuidOrNull();

            if ( groupRoleAttributeGuid.HasValue )
            {
                groupRoleGuid = action.GetWorklowAttributeValue( groupRoleAttributeGuid.Value ).AsGuidOrNull();
            }

            return groupRoleGuid;
        }
    }
}