using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using church.ccv.CCVRest.Common.Model;
using Newtonsoft.Json;
using Rock.Data;
using Rock.Model;

namespace church.ccv.CCVRest.Common
{
    public class Util
    {
        // Utility function for handling the response model
        public static HttpResponseMessage GenerateResponse( bool success, string message, object data )
        {
            ResponseModel response = new ResponseModel
            {
                Success = success,
                Message = message,
                Data = data
            };

            StringContent restContent = new StringContent( JsonConvert.SerializeObject( response ), Encoding.UTF8, "application/json" );
            return new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = restContent
            };
        }

        public static bool CreateAttendanceRecord(
            int personId,
            int? campusId,
            int attendanceGroupId,
            DateTime startDateTime,
            AttendanceService attendanceService,
            PersonAliasService personAliasService,
            RockContext rockContext
        )
        {
            // if we already have an attendance record for this start time, don't count it again.
            DateTime beginDate = startDateTime.Date;
            DateTime endDate = beginDate.AddDays( 1 );

            Attendance attendance = attendanceService.Queryable( "Group,PersonAlias.Person" )
                .Where( a =>
                    a.StartDateTime >= beginDate &&
                    a.StartDateTime < endDate &&
                    a.GroupId == attendanceGroupId &&
                    a.PersonAlias.PersonId == personId )
                .FirstOrDefault();

            if ( attendance == null )
            {
                PersonAlias primaryAlias = personAliasService.GetPrimaryAlias( personId );
                if ( primaryAlias != null )
                {
                    attendance = rockContext.Attendances.Create();
                    attendance.CampusId = campusId;
                    attendance.GroupId = attendanceGroupId;
                    attendance.PersonAlias = primaryAlias;
                    attendance.PersonAliasId = primaryAlias.Id;
                    attendance.StartDateTime = startDateTime;
                    attendance.DidAttend = true;
                    attendanceService.Add( attendance );

                    return true;
                }
            }

            return false;
        }

        public static bool HasAttendanceRecord(
            int personId,
            int attendanceGroupId,
            DateTime startDateTime,
            AttendanceService attendanceService,
            RockContext rockContext
        )
        {
            // see if there's an attendance record on the provided startDateTime and within the group.
            DateTime beginDate = startDateTime.Date;
            DateTime endDate = beginDate.AddDays( 1 );

            Attendance attendance = attendanceService.Queryable( "Group,PersonAlias.Person" )
                .Where( a =>
                    a.StartDateTime >= beginDate &&
                    a.StartDateTime < endDate &&
                    a.GroupId == attendanceGroupId &&
                    a.PersonAlias.PersonId == personId )
                .FirstOrDefault();

            return attendance != null;
        }
    }
}
