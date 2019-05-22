using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace church.ccv.CCVRest.MobileApp.Model
{
    [Serializable]
    public class MASeriesModel
    {
        public string Name;

        public string Description;

        public string ImageURL;

        public string ThumbnailURL;

        public string DateRange;

        public List<MobileAppMessageModel> Messages;

        // This will be true if EITHER:
        //1. The Podcast Series has "Active" set to false
        //2. All the Series Messages are Hidden (which has its own crieteria)
        public bool Hidden;
    }
}
