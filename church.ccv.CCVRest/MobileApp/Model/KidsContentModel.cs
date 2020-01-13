using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace church.ccv.CCVRest.MobileApp.Model
{
    [Serializable]
    public class KidsContentModel
    {
        [JsonIgnore]
        public int SortPriority; //Only used to quickly sort the results before sending to the client

        public string AtCCV_Title;
        public DateTime AtCCV_Date;
        public string AtCCV_Content;
        public string AtCCV_ImageURL;
        public string AtCCV_DiscussionTopic_One;
        public string AtCCV_DiscussionTopic_Two;

        public string FaithBuilding_Title;
        public string FaithBuilding_Content;

        public List<KidsResourceModel> Resources;
    }

    [Serializable]
    public class KidsResourceModel
    {
        [JsonIgnore]
        public int SortPriority; //Only used to quickly sort the results before sending to the client

        public string Title;
        public string Subtitle;
        public string URL;
        public bool LaunchesExternalBrowser;
    }
}
