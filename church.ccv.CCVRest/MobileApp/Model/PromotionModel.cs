
using System;

namespace church.ccv.CCVRest.MobileApp.Model
{
    [Serializable]
    public class Promotion
    {
        public int SortPriority;

        public string ImageURL;

        public string Title;
        public string Description;

        public string DetailsURL;
        public bool DetailsURLLaunchesBrowser;
        public bool IncludeAccessToken;

        public bool SkipDetailsPage;

        public DateTime? StartDateTime;
        public DateTime? EndDateTime;

        public int PublishedStatus;
    }
}