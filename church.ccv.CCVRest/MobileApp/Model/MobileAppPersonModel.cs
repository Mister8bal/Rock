using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace church.ccv.CCVRest.MobileApp.Model
{
    [Serializable]
    public class MobileAppPersonModel
    {
        // Person Info

        // NOTE - Although this is a nullable field on a person, it cannot actually be null in practice.
        public int PrimaryAliasId;

        public string FirstName;

        public string LastName;

        public string Email;

        public string PhoneNumberDigits;

        public string PhotoURL;

        public bool IsChild;

        public DateTime? Birthdate;

        public int? Age;

        public int? CampusId;


        // Family Info
        public int FamilyId;
        public List<FamilyMemberModel> FamilyMembers;
        public bool FamilyHasChildren;

        // Group Info
        public List<MobileAppGroupModel> Groups { get; set; }
        

        // Address Info
        public string Street1;

        public string Street2;

        public string City;

        public string State;

        public string Zip;


        // Next Steps
        public bool? IsBaptised;
                   
        public bool? IsWorshipping;
                   
        public bool? IsGiving;
                   
        public bool? IsServing;
                   
        public bool? IsConnected;
                   
        public bool? IsCoaching;
                   
        public bool? SharedStory;
    }
}
