
using System;
using Newtonsoft.Json;

namespace church.ccv.CCVRest.MobileApp.Model
{
    [Serializable]
    public class Promotion
    {
        [JsonIgnore]
        public int SortPriority; //Only used to quickly sort the results before sending to the client

        public string ImageURL;

        public string ThumbnailImageURL;

        public string Title;

        public string Description;

        public string DetailsURL;

        public bool SkipDetailsPage;

        public bool LaunchesExternalBrowser;

        public bool IncludeAccessToken;
    }
}