
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using Rock.Data;

namespace church.ccv.SafetySecurity.Model
{
    [Table( "_church_ccv_SafetySecurity_VolunteerScreening" )]
    [DataContract]
    public class VolunteerScreening : Model<VolunteerScreening>, IRockEntity
    {
        [DataMember]
        public int PersonAliasId { get; set; }

        [DataMember]
        public int? Application_WorkflowTypeId { get; set; }

        [DataMember]
        public int? Application_WorkflowId { get; set; }

        [DataMember]
        public DateTime? BGCheck_Result_Date { get; set; }

        [DataMember]
        public Guid? BGCheck_Result_DocGuid { get; set; }

        [DataMember]
        public string BGCheck_Result_Value { get; set; }

        #region Virtual Properties

        /// <summary>
        /// Gets or sets the person alias.
        /// </summary>
        /// <value>
        /// The person alias.
        /// </value>
        [LavaInclude]
        public virtual Rock.Model.PersonAlias PersonAlias { get; set; }

        /// <summary>
        /// Gets or sets the workflow.
        /// </summary>
        /// <value>
        /// The workflow.
        /// </value>
        [LavaInclude]
        public virtual Rock.Model.Workflow Application_Workflow { get; set; }

        #endregion

        public const string sState_InReviewWithCampus = "In Review with Campus";
        public const string sState_InReviewWithSecurity = "In Review with Security";
        public const string sState_Waiting = "Waiting for Applicant to Complete";
        public const string sState_Accepted = "In Process with Security";

        public static string GetState( DateTime? sentDate, DateTime? completedDate, string workflowStatus )
        {
            // there are 4 overall states for the screening process:
            // default - It's out there waiting for the applicant to complete
            // Campus - it's been submitted and is under review by the campus / STARS
            // Security - It's been approved by the campus and sent to security for review
            // Completed - It's been accepted by security.
            switch ( workflowStatus )
            {
                default:
                    return sState_Waiting;
                case "Campus":
                    return sState_InReviewWithCampus;
                case "Security":
                    return sState_InReviewWithSecurity;
                case "Completed":
                    return sState_Accepted;
            }
        }
    }
}
