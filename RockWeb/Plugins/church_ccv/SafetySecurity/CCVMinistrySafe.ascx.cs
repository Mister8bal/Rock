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
    [IntegerField( "Passing Score", "The score needed on Ministry Safe assesment to be considered passing", true, 70, "", 1 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Connection Status", "The connection status to use for new individuals (default: 'Web Prospect'.)", true, false, Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT, "", 0 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_RECORD_STATUS, "Record Status", "The record status to use for new individuals (default: 'Pending'.)", true, false, Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING, "", 1 )]
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
            [Name( "Renewal date (Sexual Abuse Awareness Training)" )]
            public string RenewalDate { get; set; }
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
            var binaryFile = binaryFileService.Get( fuImport.BinaryFileId ?? 0 );

            string connectionStatusBlockValue = GetAttributeValue( "ConnectionStatus" );
            string recordStatusBlockValue = GetAttributeValue( "RecordStatus" );

            if ( binaryFile != null )
            {
                using ( var reader = new StreamReader( binaryFile.ContentStream ) )
                {
                    using ( var csvReader = new CsvHelper12.CsvReader( reader ) )
                    {
                        csvReader.Configuration.HasHeaderRecord = true;
                        csvReader.Configuration.IgnoreBlankLines = true;
                        var peopleList = csvReader.GetRecords<MinistrySafePerson>().ToList();
                        Person ministrySafePerson = null;
                        bool importErrors = false;

                        foreach ( var mSafePerson in peopleList )
                        {
                            using ( var personRockContext = new RockContext() )
                            {
                                // Get passing score from rock
                                int passingTrainingScore = GetAttributeValue( "PassingScore" ).AsInteger();

                                PersonService personService = new PersonService( personRockContext );
                                var attributeValueService = new AttributeValueService( personRockContext );

                                int? trainingScore = mSafePerson.TrainingScore;
                                string trainingStatus = mSafePerson.TrainingStatus;
                                DateTime renewalDate;
                                bool renewalDateParseResult = DateTime.TryParse( mSafePerson.RenewalDate, out renewalDate );

                                // Try to find matching person
                                var personMatches = personService.GetByMatch( mSafePerson.FirstName, mSafePerson.LastName, mSafePerson.EmailAddresses );
                                if ( personMatches.Count() == 1 )
                                {
                                    // If one person with same name and email address exists, use that person
                                    ministrySafePerson = personMatches.First();
                                }
                                // If person was not found, create a new one
                                else
                                {
                                    // create new person 
                                    ministrySafePerson = new Person();
                                    ministrySafePerson.FirstName = mSafePerson.FirstName;
                                    ministrySafePerson.LastName = mSafePerson.LastName;
                                    ministrySafePerson.Email = mSafePerson.EmailAddresses;

                                    // Set record type, record status and connection status 
                                    ministrySafePerson.RecordTypeValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                                    ministrySafePerson.RecordStatusValueId = DefinedValueCache.Read( recordStatusBlockValue.AsGuid() ).Id;
                                    ministrySafePerson.ConnectionStatusValueId = DefinedValueCache.Read( connectionStatusBlockValue.AsGuid() ).Id;

                                    // save
                                    PersonService.SaveNewPerson( ministrySafePerson, personRockContext );

                                    // Create Note
                                    var noteService = new NoteService( personRockContext );

                                    var note = new Note();
                                    note.NoteTypeId = 8;
                                    note.IsSystem = false;
                                    note.IsAlert = false;
                                    note.IsPrivateNote = false;
                                    note.EntityId = ministrySafePerson.Id;
                                    note.Caption = string.Empty;
                                    note.Text = "Added by Ministry Safe import.";

                                    // Add note to persons profile
                                    noteService.Add( note );
                                }

                                // Load Attributes
                                ministrySafePerson.LoadAttributes();

                                // If the training has been completed.
                                if ( trainingStatus == "Completed" )
                                {
                                    // If the score is greater than or equal to 70.
                                    if ( trainingScore >= passingTrainingScore )
                                    {
                                        // AttributeValueService
                                        Rock.Attribute.Helper.SaveAttributeValue( ministrySafePerson, ministrySafePerson.Attributes["MinistrySafeResult"], "Pass" );
                                        Rock.Attribute.Helper.SaveAttributeValue( ministrySafePerson, ministrySafePerson.Attributes["MinistrySafeStatus"], "Completed" );

                                        // IF the renewal date from ministry safe is not blank then set and save the attribute to the persons profile.
                                        if ( renewalDateParseResult )
                                        {
                                            Rock.Attribute.Helper.SaveAttributeValue( ministrySafePerson, ministrySafePerson.Attributes["MinistrySafeRenewalDate"], renewalDate.ToString() );
                                        }
                                        // We set to true to show that renewal date was empty and shouldn't be
                                        else
                                        {
                                            importErrors = true;
                                        }  
                                    }
                                    // If the score is less than 70
                                    else
                                    {
                                        Rock.Attribute.Helper.SaveAttributeValue( ministrySafePerson, ministrySafePerson.Attributes["MinistrySafeResult"], "Fail" );
                                        Rock.Attribute.Helper.SaveAttributeValue( ministrySafePerson, ministrySafePerson.Attributes["MinistrySafeStatus"], "Completed" );
                                        Rock.Attribute.Helper.SaveAttributeValue( ministrySafePerson, ministrySafePerson.Attributes["MinistrySafeRenewalDate"], "" );
                                    }
                                }
                                else
                                {
                                    // If training is not complete, mark status Incomplete, and both result and renewal date fields will be blank
                                    Rock.Attribute.Helper.SaveAttributeValue( ministrySafePerson, ministrySafePerson.Attributes["MinistrySafeResult"], "" );
                                    Rock.Attribute.Helper.SaveAttributeValue( ministrySafePerson, ministrySafePerson.Attributes["MinistrySafeStatus"], "Incomplete" );
                                    Rock.Attribute.Helper.SaveAttributeValue( ministrySafePerson, ministrySafePerson.Attributes["MinistrySafeRenewalDate"], "" );
                                }
                            }
                        }
                        if ( importErrors )
                        {
                            pnlError.Visible = true;
                        }
                        else
                        {
                            pnlDone.Visible = true;
                        }
                    }
                }
            }
        }
    }
}