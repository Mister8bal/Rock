using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace church.ccv.CCVRest.CCVLive.Model
{
    [Serializable]
    public class AttendanceModel
    {
        public int EntityId { get; set; }
        public int InteractionComponentId { get; set; }
        public string Operation { get; set; }
        public int PersonAliasId { get; set; }
        public int InteractionSessionId { get; set; }
        public string requestTime { get; set; }
    }
    
}
