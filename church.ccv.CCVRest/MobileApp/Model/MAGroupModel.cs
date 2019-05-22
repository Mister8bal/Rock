using System;
using System.Collections.Generic;

namespace church.ccv.CCVRest.MobileApp.Model
{
    [Serializable]
    public class MAGroupModel
    {
        public int Id;

        public string Name;
        public string Description;

        public double Latitude;
        public double Longitude;
        public double DistanceFromSource;

        public string MeetingTime;

        public string Street;
        public string City;
        public string State;
        public string Zip;

        public string PhotoURL;

        public bool Childcare;
        public string ChildcareDesc;

        public List<MAGroupMemberModel> Members;
    }

    public enum MAGroupRole
    {
        AssociatePastor,
        Coach,
        Member
    }

    [Serializable]
    public class MAGroupMemberModel
    {
        public int PrimaryAliasId;
        public string Name;
        public string PhotoURL;
        public string PhoneNumberDigits;
        public string Email;
        public MAGroupRole Role;
    }
}
