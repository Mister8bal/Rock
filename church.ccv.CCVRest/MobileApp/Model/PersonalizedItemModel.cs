using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace church.ccv.CCVRest.MobileApp.Model
{
    [Serializable]
    public class PersonalizedItem
    {
        public string ImageURL;

        public string Title;

        public string Description;

        public string DetailsURL;

        public bool LaunchExternalBrowser;

        public bool IncludeAccessToken;
    }
}
