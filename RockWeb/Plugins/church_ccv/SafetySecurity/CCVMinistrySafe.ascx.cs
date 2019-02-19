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
            public string Email { get; set; }
            public string SexualAbuseAwarenessTraining { get; set; }
            public int TrainingScore { get; set; }
            public DateTime RenewalDateSexualAbuseAwarenessTraining { get; set; }
            //public string CriminalBackgroundChecklevel { get; set; }
            //public string CriminalBackgroundCheckStatus { get; set; }
            //public string Tag { get; set; }
            //public string ClassificationEmpVol { get; set; }
            //public string Charges { get; set; }
            //public string Application { get; set; }
            //public string References { get; set; }
            //public string Interview { get; set; }
            //public string Role { get; set; }
            //public DateTime RenewalDateCriminalBackgroundCheck { get; set; }

        }

        /// <summary>
        /// Loads Ministry Safe Information from File
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<MinistrySafePerson> LoadCsvFile<MinistrySafePerson>( string filePath )
        {
            var rockContext = new RockContext();
            PersonService personService = new PersonService( rockContext );

            using ( var reader = new StreamReader( Directory.GetCurrentDirectory() + @"\file.csv" ) ) //Directory.GetCurrentDirectory() + @"\file.csv";
            using ( var csvReader = new CsvReader( reader ) )
            {
                csvReader.Configuration.HasHeaderRecord = true;
                var peopleList = csvReader.GetRecords<MinistrySafePerson>().ToList();
                Person currentPerson = null;

                foreach ( var person in peopleList )
                {
                    // MinistrySafePerson roww;

                    string firstName = ""; //person.FirstName;
                    string lastName = ""; //person.LastName;
                    string email = ""; //person.Email;
                    int trainingScore = 5; //person.TrainingScore;
                    string sexualAbuseAwarenessTraining = ""; // person.SexualAbuseAwarenessTraining;
                    DateTime renewalDateSexualAbuseAwarenessTraining = DateTime.Now; //person.RenewalDateSexualAbuseAwarenessTraining;

                    // Try to find matching person
                    var personMatches = personService.GetByMatch( firstName, lastName, email );
                    if ( personMatches.Count() == 1 )
                    {
                        // If one person with same name and email address exists, use that person
                        currentPerson = personMatches.First();
                    }

                    // If person was not found, create a new one
                    else if ( person == null )
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
                        // Get attributes in rock
                        var ministrySafeResult = currentPerson.AttributeValues[ "MinistrySafeResult" ].ToString(); // Pass or Fail
                        var ministrySafeStatus = currentPerson.AttributeValues[ "MinistrySafeStatus"].ToString(); // Complete or Incomplete
                        DateTime? ministrySafeRenewalDate = currentPerson.GetAttributeValue( "MinistrySafeRenewalDate" ).AsDateTime();

                        // If the training has been completed.
                        if ( sexualAbuseAwarenessTraining == "Complete" )
                        {
                            // If the score is greater than or equal to 70.
                            if ( trainingScore >= 70 )
                            {
                                ministrySafeResult = "Pass";
                                ministrySafeStatus = "Complete";
                                ministrySafeRenewalDate = renewalDateSexualAbuseAwarenessTraining;
                            }
                            // If the score is less than 70
                            else if ( trainingScore < 70 )
                            {
                                ministrySafeResult = "Fail";
                                ministrySafeStatus = "Complete";
                                ministrySafeRenewalDate = null;
                            }
                        }
                        else
                        {
                            // If training is not complete, mark status Incomplete, and both result and renewal date fields will be blank
                            ministrySafeStatus = "Incomplete";
                            ministrySafeResult = null;
                            ministrySafeRenewalDate = null;
                        }
                        return peopleList;
                    }
                }

                return peopleList;
            }
        }

    }
}