using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI.WebControls;
// CCV CORE - JHM - 2-28-19 - This file needs to use the latest (v12 as of this writing) version of CsvHelper to function correctly. However, Rock.Slingshot is using CsvHelper v2, so we can't upgrade it without changing core.
// So we downloaded the source code for v12, changed the namespace to CsvHelper12, and bundled that into Rock's libs folder, and reference it here in the website.
using CsvHelper12;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using System.IO;
using Rock.Web.Cache;
using CsvHelper12.Configuration.Attributes;

namespace RockWeb.Plugins.church_ccv.SafetySecurity
{
    /// <summary>
    /// Imports Ministry Safe Records
    /// </summary>
    [DisplayName( "CCV Ministry Safe" )]
    [Category( "CCV > Safety and Security" )]
    [Description( "Imports CCV STARS Coaches Ministry Safe Results." )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Connection Status", "The connection status to use for new individuals (default: 'Web Prospect'.)", true, false, Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT, "", 0 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_RECORD_STATUS, "Record Status", "The record status to use for new individuals (default: 'Pending'.)", true, false, Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING, "", 1 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.FINANCIAL_SOURCE_TYPE, "Source", "The Financial Source Type to use when imported from Ministry Safe", false, false, Rock.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_WEBSITE, "", 2 )]
    public partial class CCVMinistrySafe : RockBlock
    {
        public class MinistrySafePerson
        {
            [Name("First Name")]
            public string FirstName { get; set; }
            [Name( "Last Name" )]
            public string LastName { get; set; }
            [Name( "Email Addresses" )]
            public string EmailAddresses { get; set; }
            [Name( "Sexual Abuse Awareness Training completed/incompleted" )]
            public string TrainingStatus { get; set; }
            [Name( "Renewal Date (Criminal Background Check)" )]
            public string RenewalDate { get; set; } //Datetime?
            [Name( "Sexual Abuse Awareness Training Score" )]
            public int? TrainingScore { get; set; }
        }

        /// <summary>
        /// Handles the FileUploaded event of the fuImport control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void fuImport_FileUploaded( object sender, EventArgs e )
        {
            pnlStart.Visible = false;
            pnlDone.Visible = false;

            var rockContext = new RockContext();
            var binaryFileService = new BinaryFileService( rockContext );
            PersonService personService = new PersonService( rockContext );
            var attributeValueService = new AttributeValueService( rockContext );

            var binaryFile = binaryFileService.Get( fuImport.BinaryFileId ?? 0 );

            // Delete file? Database problems

            if ( binaryFile != null )
            {
                using ( var reader = new StreamReader( binaryFile.ContentStream ) )
                using ( var csvReader = new CsvHelper12.CsvReader( reader ) )
                {
                    csvReader.Configuration.HasHeaderRecord = true;
                    csvReader.Configuration.IgnoreBlankLines = true;
                    var peopleList = csvReader.GetRecords<MinistrySafePerson>().ToList();
                    Person currentPerson = null;

                    foreach ( var mSafePerson in peopleList )
                    {
                        int? trainingScore = mSafePerson.TrainingScore;
                        string trainingStatus = mSafePerson.TrainingStatus;
                        DateTime renewalDate;
                        DateTime.TryParse(mSafePerson.RenewalDate, out renewalDate );

                        // Try to find matching person
                        var personMatches = personService.GetByMatch( mSafePerson.FirstName, mSafePerson.LastName, mSafePerson.EmailAddresses );
                        if ( personMatches.Count() == 1 )
                        {
                            // If one person with same name and email address exists, use that person
                            currentPerson = personMatches.First();
                        }

                        // If person was not found, create a new one
                        else
                        {
                            // create new person 
                            currentPerson = new Person();
                            currentPerson.FirstName = mSafePerson.FirstName;
                            currentPerson.LastName = mSafePerson.LastName;
                            currentPerson.Email = mSafePerson.EmailAddresses;

                            int recordTypePersonId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                            int recordStatusActiveId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() ).Id;
                            int connectionStatusActiveId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT.AsGuid() ).Id;

                            // Set record type, record status and connection status 
                            currentPerson.RecordTypeValueId = recordTypePersonId;
                            currentPerson.RecordStatusValueId = recordStatusActiveId;
                            currentPerson.ConnectionStatusValueId = connectionStatusActiveId;

                            // Create Note and Person Service
                            var noteService = new NoteService( rockContext );
                            var personAliasService = new PersonAliasService( rockContext );

                            var note = new Note();
                            note.NoteTypeId = 36;
                            note.IsSystem = false;
                            note.IsAlert = false;
                            note.IsPrivateNote = false;
                            note.EntityId = currentPerson.Id;
                            note.Caption = string.Empty;

                            if ( personMatches.Count() > 1 )
                            {
                                foreach ( var peeps in personMatches )
                                {
                                    // Write note saying that two people matches were found so we created a new person instead.
                                    note.Text = string.Format( "Person might exist already: " + "<a href=/Person/" + personMatches.First().Id + ">" + personMatches.First().FullName + "</a> . If it's not them, please discard note."  );
                                }
                            }

                            else
                            {
                                // Else just write a regular note on person profile saying "added by ministry safe"
                                note.Text = "Added by Ministry Safe.";
                            }

                            // Add note to persons profile
                            noteService.Add( note );
                        }
                       
                        // save
                        PersonService.SaveNewPerson( currentPerson, rockContext );

                        // If there is a valid person with a primary alias, continue
                        if ( currentPerson != null && currentPerson.PrimaryAliasId.HasValue ) // remove after refactor
                        {
                            currentPerson.LoadAttributes();

                            // Get attributes in rock
                            string ministrySafeResult = currentPerson.AttributeValues["MinistrySafeResult"].Value; // Pass or Fail
                            string ministrySafeStatus = currentPerson.AttributeValues["MinistrySafeStatus"].Value; // Complete or Incomplete
                            var MinistrySafeRenewalDate = currentPerson.AttributeValues["MinistrySafeRenewalDate"].Value; // Date

                            // If the training has been completed.
                            if ( trainingStatus == "Completed" )
                            {
                                // If the score is greater than or equal to 70.
                                if ( trainingScore >= 70 )
                                {
                                    // AttributeValueService
                                    Rock.Attribute.Helper.SaveAttributeValue( currentPerson, currentPerson.Attributes["MinistrySafeResult"], "Pass" );
                                    Rock.Attribute.Helper.SaveAttributeValue( currentPerson, currentPerson.Attributes["MinistrySafeStatus"], "Completed" );
                                    Rock.Attribute.Helper.SaveAttributeValue( currentPerson, currentPerson.Attributes["MinistrySafeRenewalDate"], renewalDate.ToString() );
                                }
                                // If the score is less than 70
                                else 
                                {
                                    Rock.Attribute.Helper.SaveAttributeValue( currentPerson, currentPerson.Attributes["MinistrySafeResult"], "Fail" );
                                    Rock.Attribute.Helper.SaveAttributeValue( currentPerson, currentPerson.Attributes["MinistrySafeStatus"], "Completed" );
                                    Rock.Attribute.Helper.SaveAttributeValue( currentPerson, currentPerson.Attributes["MinistrySafeRenewalDate"], "" );
                                }
                            }
                            else
                            {
                                // If training is not complete, mark status Incomplete, and both result and renewal date fields will be blank
                                Rock.Attribute.Helper.SaveAttributeValue( currentPerson, currentPerson.Attributes["MinistrySafeResult"], "" );
                                Rock.Attribute.Helper.SaveAttributeValue( currentPerson, currentPerson.Attributes["MinistrySafeStatus"], "Incomplete" );
                                Rock.Attribute.Helper.SaveAttributeValue( currentPerson, currentPerson.Attributes["MinistrySafeRenewalDate"], "" );
                            }
                            currentPerson.SaveAttributeValues( rockContext );
                            rockContext.SaveChanges();
                        }
                    }
                }
            }
        }
    }
}