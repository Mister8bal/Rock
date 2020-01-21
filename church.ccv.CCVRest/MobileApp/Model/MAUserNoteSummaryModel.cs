using System;

namespace church.ccv.CCVRest.MobileApp.Model
{
    [Serializable]
    public class MAUserNoteSummaryModel
    {
        // These will be enabled once we move to cloud note saving
        //public int UserNoteId;
        //public int MessageId;

        public string SeriesName;
        public string SeriesDateRange;
        public string SeriesImageURL;

        public string MessageName;
        public string MessageSpeaker;
        public string MessageNoteURL;
    }
}
