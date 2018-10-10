using Rock.Data;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace church.ccv.PersonalizationEngine.Model
{
    [Table( "_church_ccv_PersonalizationEngine_CampaignType" )]
    [DataContract]
    public class CampaignType : Model<CampaignType>, IRockEntity
    {
        [DataMember]
        public string Name { get; set; }
        
        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string DebugUrl { get; set; }

        [DataMember]
        public string JsonTemplate { get; set; }
    }
}
