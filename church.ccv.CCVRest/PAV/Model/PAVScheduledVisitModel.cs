using System;
using System.Collections.Generic;

namespace church.ccv.CCVRest.PAV.Model
{
    [Serializable]
    public class PAVScheduledVisitsModel
    {
        public List<PAVScheduledVisitModel> ScheduledVisits;

        public List<PAVCampusModel> Campuses;
    }

    [Serializable]
    public class PAVScheduledVisitModel
    {
        public int Id;

        // Family Info
        public string AdultOneFirstName;

        public string AdultOneLastName;

        public string AdultOneMobileNumber;

        public string AdultTwoFirstName;

        public string AdultTwoLastName;

        public List<PAVChildModel> Children;

        // Scheduled Visit Info
        public string ScheduledDate;

        public int ScheduledCampusId;

        public string ScheduledCampusName;

        public int ScheduledServiceId;

        public string ScheduledServiceName;

        // Attended Visit Info
        public string AttendedDate;

        public int? AttendedCampusId;

        public string AttendedCampusName;

        public int? AttendedServiceId;

        public string AttendedServiceName;
    }

    [Serializable]
    public class PAVChildModel
    {
        public string FirstName;

        public int? Age;

        public string BirthDate;

        public string Grade;
    }

    [Serializable]
    public class PAVCampusModel
    {
        public int Id;

        public string Name;

        public List<PAVServiceTimeModel> ServiceTimes;
    }

    [Serializable]
    public class PAVServiceTimeModel
    {
        public int ScheduleId;

        public string Day;

        public string Name;

        public string Time;
    }
}
