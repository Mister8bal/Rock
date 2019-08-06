using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace church.ccv.CCVRest.MobileApp.Model
{
    [Serializable]
    public class MobileAppMessageModel
    {
        public int Id;

        public string Name;

        public string Speaker;

        public DateTime Date;

        // This will always be the same as the Parent Series but is here for convenience
        public string ImageURL;

        public string NoteURL;
        public string VideoURL;
        public string ResourcesHTML;
        public string DiscussionGuideURL;

        // This will be true if EITHER:
        //1. The Message's "Approved" (using the horribly named "Active" key) is set to False
        //2. The StartDateTime (confusingly labeled "Active") is in the future.
        public bool Hidden;
    }
}
