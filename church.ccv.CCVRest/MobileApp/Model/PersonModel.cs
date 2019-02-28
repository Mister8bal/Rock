using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace church.ccv.CCVRest.MobileApp.Model
{
    [Serializable]
    public class PersonModel
    {
        // Person Info
        public int PrimaryAliasId;

        public string FirstName;

        public string LastName;

        public string Email;

        public string PhoneNumberDigits;

        public int? PhotoId;

        public DateTime? Birthdate;


        // Family Info
        [Serializable]
        public class FamilyMember
        {
            public int PrimaryAliasId;

            public string FirstName;

            public string LastName;

            public int? PhotoId;
        }
        public int FamilyId;
        public List<FamilyMember> FamilyMembers;
        

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
