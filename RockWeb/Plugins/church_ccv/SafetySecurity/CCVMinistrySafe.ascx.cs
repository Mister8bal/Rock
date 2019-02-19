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
            public string MinistrySafeStatus { get; set; }
            public string CriminalBackgroundChecklevel { get; set; }
            public string CriminalBackgroundCheckStatus { get; set; }
            public string Tag { get; set; }
            public string ClassificationEmpVol { get; set; }
            public string Charges { get; set; }
            public string Application { get; set; }
            public string References { get; set; }
            public string Interview { get; set; }
            public string RenewaldateSexualAbuseAwarenessTraining { get; set; }
            public string Role { get; set; }
            public DateTime RenewalDateCriminalBackgroundCheck { get; set; }
            public int TrainingScore { get; set; }

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
                Person person = null;


                foreach ( var row in peopleList )
                {
                    MinistrySafePerson roww;
                    
                    string firstName = roww.FirstName;
                    string lastName = tbLastName.Text.Trim();
                    string email = tbEmail.Text.Trim();

                    // Try to find matching person
                    var personMatches = personService.GetByMatch( row.FirstName, row.LastName, row.Email );
                    if ( personMatches.Count() == 1 )
                    {
                        // If one person with same name and email address exists, use that person
                        person = personMatches.First();
                    }
              
                    // If person was not found, create a new one
                    else if ( person == null )
                    {   
                        person = new Person();
                        person.FirstName = firstName;
                        person.LastName = lastName;
                        person.Email = email;
                        PersonService.SaveNewPerson( person, rockContext);
                    }




                    // If there is a valid person with a primary alias, continue
                    if ( person != null && person.PrimaryAliasId.HasValue )
                    {

                        // Get next family members details
                        var ministrySafeResult = person.GetAttributeValues( "MinistrySafeResult" );
                        DateTime? ministrySafeRenewalDate = person.GetAttributeValue( "MinistrySafeRenewalDate" ).AsDateTime();
                        string ministrySafeStatus = person.AttributeValues["MinistrySafeStatus"].ToString();

                        // Assign baptism photo
                        if ( SexualAbuseAwarenessTrainingCompletedIncompleted == 'Complete' && TrainingScore >= 70 )
                        {
                            ministrySafeResult.Text = "Pass";
                        }
                        else
                        {
                            lBaptismPhoto.Text = "";
                        }


                        return people;
            }

        }
      










    //private List<MinistrySafePerson> LoadCsvFile<MinistrySafePerson>( string filePath )
    //{

    //    var fileName = Path.Combine( this.SlingshotDirectoryName, new T().GetFileName() );
    //    if ( File.Exists( fileName ) )
    //    {
    //        using ( var fileStream = File.OpenText( fileName ) )
    //        {
    //            CsvReader csvReader = new CsvReader( fileStream );
    //            csvReader.Configuration.HasHeaderRecord = true;
    //            return csvReader.GetRecords<MinistrySafePerson>().ToList();
    //        }
    //    }
    //    else
    //    {
    //        return new List<MinistrySafePerson>();
    //    }
    //}



}
}