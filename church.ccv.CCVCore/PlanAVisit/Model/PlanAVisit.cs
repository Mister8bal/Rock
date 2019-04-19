using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Rock.Data;

namespace church.ccv.CCVCore.PlanAVisit.Model
{
    [Table( "_church_ccv_PlanAVisit ")]
    [DataContract]
    public class PlanAVisit : Model<PlanAVisit>, IRockEntity
    {
        [DataMember]
        public int PersonAliasId { get; set; }

        [DataMember]
        public int FamilyId { get; set; }

        [DataMember]
        public int CampusId { get; set; }

        [DataMember]
        public DateTime? ScheduledDate { get; set; }

        [DataMember]
        public DateTime? AttendedDate { get; set; }

        [DataMember]
        public int ServiceTimeScheduleId { get; set; }

        [DataMember]
        public bool BringingSpouse { get; set; }

        [DataMember]
        public bool BringingChildren { get; set; }

        [DataMember]
        public string SurveyResponse { get; set; }



    }
}
