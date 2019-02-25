using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI.WebControls;
using CsvHelper;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using System.IO;
using Rock.Web.Cache;

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
    [DefinedValueField( Rock.SystemGuid.DefinedType.FINANCIAL_SOURCE_TYPE, "Source", "The Financial Source Type to use when creating transactions", false, false, Rock.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_WEBSITE, "", 2 )]
    [LinkedPage( "Detail Page" )]
    public partial class CCVMinistrySafe : RockBlock
    {

        public class MinistrySafePerson
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string EmailAddresses { get; set; }
            public string SexualAbuseAwarenessTrainingcompletedincompleted { get; set; }
            public string CriminalBackgroundChecklevel { get; set; }
            public string CriminalBackgroundCheckstatus{ get; set; }
            public string Tag { get; set; }
            public string ClassificationEmpVol { get; set; }
            public string Charges { get; set; }
            public string Application { get; set; }
            public string References { get; set; }
            public string Interview { get; set; }
            public string RenewaldateSexualAbuseAwarenessTraining{ get; set; } //Datetime?
            public string Role { get; set; }
            public string RenewalDateCriminalBackgroundCheck { get; set; } // Datetime?
            public int SexualAbuseAwarenessTrainingScore { get; set; }
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

            var binaryFile = binaryFileService.Get( fuImport.BinaryFileId ?? 0 );

            var dvcConnectionStatus = DefinedValueCache.Read( GetAttributeValue( "ConnectionStatus" ).AsGuid() );
            var dvcRecordStatus = DefinedValueCache.Read( GetAttributeValue( "RecordStatus" ).AsGuid() );
            // Delete file? Database problems

            // member status and connection status: block attributes

            if ( binaryFile != null )
            {
                using ( var reader = new StreamReader( binaryFile.ContentStream ) )
                    using ( var csvReader = new CsvReader( reader ) )
                    {
                        csvReader.Configuration.HasHeaderRecord = true;
                        csvReader.Configuration.TrimFields = true;
                        csvReader.Configuration.IgnoreBlankLines = true;
                        var peopleList = csvReader.GetRecords<MinistrySafePerson>().ToList();
                        Person currentPerson = null;

                        foreach ( var mSafePerson in peopleList )
                        {
                          
                            int trainingScore = mSafePerson.SexualAbuseAwarenessTrainingScore;
                            string sexualAbuseAwarenessTraining = mSafePerson.SexualAbuseAwarenessTrainingcompletedincompleted;
                            DateTime renewalDateSexualAbuseAwarenessTraining = DateTime.Parse( mSafePerson.RenewaldateSexualAbuseAwarenessTraining);

                            // Try to find matching person
                            var personMatches = personService.GetByMatch( mSafePerson.FirstName, mSafePerson.LastName, mSafePerson.EmailAddresses );
                            if ( personMatches.Count() == 1 )
                            {
                                // If one person with same name and email address exists, use that person
                                currentPerson = personMatches.First();
                            }

                            // If person was not found, create a new one
                            else if ( personMatches.Count() == 0 )
                            {
                                currentPerson = new Person();
                                currentPerson.FirstName = mSafePerson.FirstName;
                                currentPerson.LastName = mSafePerson.LastName;
                                currentPerson.Email = mSafePerson.EmailAddresses;

                                if ( dvcConnectionStatus != null )
                                {
                                    currentPerson.ConnectionStatusValueId = dvcConnectionStatus.Id;
                                }
                                if ( dvcRecordStatus != null )
                                {
                                    currentPerson.RecordStatusValueId = dvcRecordStatus.Id;
                                }
                                currentPerson.ConnectionStatusValueId = connectionStatusId;
                                currentPerson. = recordStatusPendingId;
                                currentPerson.RecordTypeValueId = recordTypePersonId;

                                currentPerson.SystemNote = "Added by Ministry Safe";
                                // Set status web prospect
                                // record status and connection status
                                // as block attributes
                                PersonService.SaveNewPerson( currentPerson, rockContext );
                            }

                            // if personMatches.Count() >1 ....

                            // If there is a valid person with a primary alias, continue
                            if ( currentPerson != null && currentPerson.PrimaryAliasId.HasValue ) // remove after refactor
                            {
                                currentPerson.LoadAttributes();
                                // Get attributes in rock
                                var ministrySafeResult = currentPerson.AttributeValues["MinistrySafeResult"]; // Pass or Fail
                                var ministrySafeStatus = currentPerson.AttributeValues["MinistrySafeStatus"]; // Complete or Incomplete
                                DateTime? ministrySafeRenewalDate = currentPerson.GetAttributeValue( "MinistrySafeRenewalDate" ).AsDateTime();

                                // If the training has been completed.
                                if ( sexualAbuseAwarenessTraining == "Completed" )
                                {
                                    // If the score is greater than or equal to 70.
                                    if ( trainingScore >= 70 )
                                    {
                                        // AttributeValueService
                                        SetAttributeValue( ministrySafeResult.AttributeKey, "Pass" );
                                        SetAttributeValue( ministrySafeStatus.AttributeKey, "Completed" );
                                        SetAttributeValue( ministrySafeRenewalDate.ToString(), renewalDateSexualAbuseAwarenessTraining.ToString() );
                                    }
                                    // If the score is less than 70
                                    else if ( trainingScore < 70 ) // Change to else 
                                    // make score a block setting
                                    {
                                    // find all: set attribute is being set to null??
                                        SetAttributeValue( ministrySafeResult.AttributeKey, "Fail" );
                                        SetAttributeValue( ministrySafeStatus.AttributeKey, "Completed" );
                                        SetAttributeValue( ministrySafeRenewalDate.ToString(), "" );
                                        //ministrySafeRenewalDate = null;
                                    }
                                }
                                else
                                {
                                    // If training is not complete, mark status Incomplete, and both result and renewal date fields will be blank
                                    SetAttributeValue( ministrySafeResult.AttributeKey, "" );
                                    SetAttributeValue( ministrySafeStatus.AttributeKey, "Incomplete" );
                                    SetAttributeValue( ministrySafeRenewalDate.ToString(), "" );
                                    //ministrySafeRenewalDate = null;
                            }
                            rockContext.SaveChanges();
                          }
                     }
                }
            }
        }
    }
}