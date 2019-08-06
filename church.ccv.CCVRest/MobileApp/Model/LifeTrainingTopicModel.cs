using System;
using System.Collections.Generic;

namespace church.ccv.CCVRest.MobileApp.Model
{
    [Serializable]
    public class LifeTrainingTopicModel
    {
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
    }
}
