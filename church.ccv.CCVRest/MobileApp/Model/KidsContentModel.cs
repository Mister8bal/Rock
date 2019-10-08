using System;
using System.Collections.Generic;

namespace church.ccv.CCVRest.MobileApp.Model
{
    [Serializable]
    public class KidsContentModel
    {
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
        public string Title;
        public string Subtitle;
        public string URL;
        public bool LaunchesExternalBrowser;
    }
}
