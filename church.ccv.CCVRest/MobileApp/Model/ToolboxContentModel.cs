using System;
using System.Collections.Generic;

namespace church.ccv.CCVRest.MobileApp.Model
{
    public class ToolboxContentModel
    {
        public List<ToolboxResourceModel> ResourceList;
        public APBoardModel APBoardContent;
    }

    public class ToolboxResourceModel
    {
        public int SeriesId;
        public string SeriesName;
        public string SeriesImageURL;
        public int MessageId;
        public string MessageName;
        public DateTime WeekendDate;
        public int WeekNumber;
        public string DiscussionGuideURL;
        public string VideoURL;
    }

    public class APBoardModel
    {
        public string AssociatePastorName;
        public string AssociatePastorImageURL;

        public DateTime Date;

        public string Summary;
        public string TipOfTheWeek;
        public string VideoURL;
        public DateTime VideoDate;
        public string VideoName;
        public string VideoThumbnailURL;
    }
}
