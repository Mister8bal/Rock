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
using System.Collections.Generic;

namespace RockWeb.Plugins.church_ccv.SafetySecurity
{
    /// <summary>
    /// Imports Ministry Safe Records
    /// </summary>
    [DisplayName( "CCV Ministry Safe" )]
    [Category( "CCV > Safety and Security" )]
    [Description( "Imports CCV STARS Coaches Ministry Safe Results." )]

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
            public string RenewaldateSexualAbuseAwarenessTraining{ get; set; }
            public string Role { get; set; }
            public string RenewalDateCriminalBackgroundCheck { get; set; }
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

                        foreach ( var person in peopleList )
                        {
                            string firstName = person.FirstName;
                            string lastName = person.LastName;
                            string email = person.EmailAddresses;
                            int trainingScore = person.SexualAbuseAwarenessTrainingScore;
                            string sexualAbuseAwarenessTraining = person.SexualAbuseAwarenessTrainingcompletedincompleted;
                            DateTime renewalDateSexualAbuseAwarenessTraining = DateTime.Parse( person.RenewaldateSexualAbuseAwarenessTraining);

                            // Try to find matching person
                            var personMatches = personService.GetByMatch( firstName, lastName, email );
                            if ( personMatches.Count() == 1 )
                            {
                                // If one person with same name and email address exists, use that person
                                currentPerson = personMatches.First();
                            }

                            // If person was not found, create a new one
                            else if ( personMatches.Count() == 0 )
                            {
                                currentPerson = new Person();
                                currentPerson.FirstName = firstName;
                                currentPerson.LastName = lastName;
                                currentPerson.Email = email;
                                PersonService.SaveNewPerson( currentPerson, rockContext );
                            }

                            // If there is a valid person with a primary alias, continue
                            if ( currentPerson != null && currentPerson.PrimaryAliasId.HasValue )
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
                                        SetAttributeValue( ministrySafeResult.AttributeKey, "Pass" );
                                        SetAttributeValue( ministrySafeStatus.AttributeKey, "Completed" );
                                        SetAttributeValue( ministrySafeRenewalDate.ToString(), renewalDateSexualAbuseAwarenessTraining.ToString() );
                                    }
                                    // If the score is less than 70
                                    else if ( trainingScore < 70 )
                                    {
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