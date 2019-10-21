using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace church.ccv.CCVRest.MobileApp.Model
{
    [Serializable]
    public class PersonalizedItem
    {
        [JsonIgnore]
        public int SortPriority; //Only used to quickly sort the results before sending to the client

        public string ImageURL;

        public string Title;

        public string SubTitle;

        public string DetailsBody;

        public string DetailsURL;

        public bool SkipDetailsPage;

        public bool LaunchesExternalBrowser;

        public bool IncludeAccessToken;
    }
}
