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
        public string SeriesName;
        public string SeriesImageURL;
        public string MessageName;
        public DateTime WeekendDate;
        public int WeekNumber;
        public string DiscussionGuideURL;
        public string WistiaId;
    }

    public class APBoardModel
    {
        public string AssociatePastorName;
        public string AssociatePastorImageURL;

        public DateTime Date;

        public string Summary;
        public string TipOfTheWeek;
        public string WistiaId;
    }
}
