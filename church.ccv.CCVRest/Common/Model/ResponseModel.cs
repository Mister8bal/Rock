
using System;

namespace church.ccv.CCVRest.Common.Model
{
    [Serializable]
    class ResponseModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }
}
