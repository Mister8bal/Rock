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

        public DateTime? Birthdate;

        public int? CampusId;


        // Family Info
        public int FamilyId;
        public List<FamilyMemberModel> FamilyMembers;

        // Group Info
        public List<MobileAppGroupModel> Groups { get; set; }
        

        // Address Info
        public string Street1;

        public string Street2;

        public string City;

        public string State;

        public string Zip;


        // Next Steps
        public bool IsBaptised;

        public bool IsERA;

        public bool IsGiving;

        public bool TakenStartingPoint;

        public bool IsMember;

        public bool IsServing;

        public bool IsPeerLearning;

        public bool IsMentored;

        public bool IsTeaching;

        public bool SharedStory;
    }
}
