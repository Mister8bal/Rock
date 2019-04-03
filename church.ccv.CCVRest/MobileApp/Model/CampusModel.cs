
using System;
using System.Collections.Generic;

namespace church.ccv.CCVRest.MobileApp.Model
{
    [Serializable]
    public class CampusModel
    {
        public string Name;
        public string VideoURL;

        public string CampusPastorName;
        public string CampusPastorEmail;
        public string CampusPastorImageURL;

        public string Street;
        public string City;
        public string State;
        public string Zip;

        public string PhoneNumber;

        public double Latitude;
        public double Longitude;
        public double DistanceFromSource;

        public List<ServiceTimeModel> ServiceTimes;

        public string Kids_ServiceTime;
        public string Kids_ServiceLocation;

        public string SeventhGrade_ServiceTime;
        public string SeventhGrade_ServiceLocation;

        public string EighthGrade_ServiceTime;
        public string EighthGrade_ServiceLocation;

        public string HighSchool_ServiceTime;
        public string HighSchool_ServiceLocation;

        public string Info_About;
        public string Info_FirstTimeArrival;
        public string Info_CheckingInKids;
        public List<string> Info_ParkingDirectionSteps;
        public string Info_MapImageURL;
    }

    [Serializable]
    public class ServiceTimeModel
    {
        public string Day;
        public string Time;

        public bool SpecialNeeds;
        public bool HearingImpaired;
    }
}
