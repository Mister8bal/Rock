using System;

namespace church.ccv.CCVRest.STARS.Model
{
    [Serializable]
    public class STARSRegistrationModel
    {
        public int EventOccurrenceId;

        public DateTime? EventOccurrenceDate;

        public string EventSummary;

        public int? RegistrationInstanceId;

        public string Campus;

        public string Sport;

        public string Gender;

        public string Division;

        public string Season;

        public string Grades;

        public int SlotsAvailable;

        public bool WaitListEnabled;

        public decimal? Cost;
    }
}
