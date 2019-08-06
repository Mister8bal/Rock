using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace church.ccv.CCVRest.MobileApp.Model
{
    [Serializable]
    public class MobileAppLatestMessageModel
    {
        public bool ShouldDisplayBanner;
        public MobileAppMessageModel Message;
    }
}
