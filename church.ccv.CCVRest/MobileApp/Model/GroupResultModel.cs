using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace church.ccv.CCVRest.MobileApp.Model
{
    [Serializable]
    class GroupResult
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

        public int? PhotoId;

        public bool Childcare;
        public string ChildcareDesc;

        public string AssociatePastorName;
        public int? AssociatePastorPhotoId;

        public string CoachName;
        public int? CoachPhotoId;
    }
}
