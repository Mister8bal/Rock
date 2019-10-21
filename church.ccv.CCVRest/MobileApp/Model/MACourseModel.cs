using System;
using System.Collections.Generic;

namespace church.ccv.CCVRest.MobileApp.Model
{
    [Serializable]
    public class MACourseModel
    {
        public int Id;

        public string Title;
        public string SubTitle;
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
        public string ThumbnailPhotoURL;

        public MACourseMemberModel AssociatePastor;
        public MACourseMemberModel CourseLeader;

        public bool IsFull;
    }

    [Serializable]
    public class MACourseMemberModel
    {
        public string Name;
        public string ThumbnailPhotoURL;
    }
}
