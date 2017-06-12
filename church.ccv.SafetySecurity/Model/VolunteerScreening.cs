﻿using Rock.Data;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace church.ccv.SafetySecurity.Model
{
        [Table( "_church_ccv_SafetySecurity_VolunteerScreening" )]
        [DataContract]
        public class VolunteerScreening : Model<VolunteerScreening>, IRockEntity
        {
            public enum Types
            {
                Legacy,
                Normal
            }

            [DataMember]
            public int PersonAliasId { get; set; }

            [DataMember]
            public int Type { get; set; }

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

            // These are legacy values used for scanning in old applications
            [DataMember]
            public int? Legacy_Application_DocFileId { get; set; }
        
            [DataMember]
            public int? Legacy_CharacterReference1_DocFileId { get; set; }

            [DataMember]
            public int? Legacy_CharacterReference2_DocFileId { get; set; }

            [DataMember]
            public int? Legacy_CharacterReference3_DocFileId { get; set; }
            //

            public const string sState_HandedOff = "Handed off to Security";
            public const string sState_InReview = "Application in Review";
            public const string sState_Waiting = "Waiting for Applicant to Complete";

            public static string GetState( DateTime sentDate, DateTime completedDate, string workflowStatus )
            {
                // there are 3 overall states for the screening process:
                // Application Sent (modifiedDateTime == createdDateTime)
                // Application Completed and in Review (modifiedDateTime > createdDateTime)
                // Application Approved and now being reviewed by security (workflow == complete)
                if( workflowStatus == "Completed" )
                {
                    return sState_HandedOff;
                }
                else if ( completedDate > sentDate )
                {
                    return sState_InReview;
                }
                else
                {
                    return sState_Waiting;
                }
            }
        }
}