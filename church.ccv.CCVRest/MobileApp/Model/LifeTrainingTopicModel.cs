using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace church.ccv.CCVRest.MobileApp.Model
{
    [Serializable]
    public class LifeTrainingTopicModel
    {
        [JsonIgnore]
        public int SortPriority; //Only used to quickly sort the results before sending to the client

        public string Title;
        public string Content;
        public string ImageURL;
        public string TalkToSomeoneURL;

        public List<LifeTrainingResourceModel> Resources;
    }

    [Serializable]
    public class LifeTrainingResourceModel
    {
        public string Title;
        public string Content;
        public string Author;
        public string URL;
        public string ImageURL;
        public bool IsBook;
        public bool LaunchesExternalBrowser;
    }
}
