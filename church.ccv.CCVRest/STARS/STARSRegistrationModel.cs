using System;

namespace church.ccv.CCVRest.STARS
{
    [Serializable]
    public class STARSRegistrationModel
    {
        public int EventOccurenceId;

        public int? RegistrationInstanceId;

        public string Campus;

        public string Sport;

        public string Gender;

        public string Division;

        public string Season;
    }
}
