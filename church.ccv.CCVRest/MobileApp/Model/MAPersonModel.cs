using System;
using System.Collections.Generic;

namespace church.ccv.CCVRest.MobileApp.Model
{
    [Serializable]
    public class MAPersonModel
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

        public string DisplayAge;

        public int? CampusId;


        // Family Info
        public int FamilyId;
        public List<FamilyMemberModel> FamilyMembers;
        public bool FamilyHasChildren;

        // Group Info
        public List<MAGroupModel> Groups { get; set; }
        

        // Address Info
        public string Street1;

        public string Street2;

        public string City;

        public string State;

        public string Zip;


        // Next Steps
        public bool? IsBaptised;
        public DateTime? BaptismDate;
                   
        public bool? IsWorshipping;
                   
        public bool? IsGiving;
                   
        public bool? IsServing;
                   
        public bool? IsConnected;
                   
        public bool? IsCoaching;
                   
        public bool? SharedStory;


        // Access Token (for SSO with Rock)
        public string RockAccessToken;
    }

    [Serializable]
    public class FamilyMemberModel
    {
        public int PrimaryAliasId;

        public string FirstName;

        public string LastName;

        public string Email;

        public int? Age;

        public string DisplayAge;

        public bool IsChild;

        public string ThumbnailPhotoURL;
    }
}
