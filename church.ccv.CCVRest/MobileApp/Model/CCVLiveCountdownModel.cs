using System;
using Newtonsoft.Json;

namespace church.ccv.CCVRest.MobileApp.Model
{
    class CCVLiveCountdownModel
    {
#pragma warning disable 0649 //suppress the "value never assigned" warnings, since we fill this via json deserializing
        public bool IsLive;
   
        public DateTime? EventStartTime;
#pragma warning restore 0649
    }
}
