// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System.IO;
using System.Net.Http;
using System.Text;
using Rock.Model;
using Newtonsoft.Json;
using System.Net;
using System.Web.Http;
using System.Web.Routing;
using Rock.Rest.Filters;
using Rock.Security;
using Rock.Data;
using Rock;
using System.Collections.Generic;
using System.Linq;
using System;
using church.ccv.CCVRest.CCVLive.Model;


namespace church.ccv.CCVRest.CCVLive
{
    class AttendanceController : Rock.Rest.ApiControllerBase
    {
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/CCVLive/Attendance")]
        [Authenticate, Secured]
        public HttpResponseMessage Attendance([FromBody]AttendanceModel attendanceModel)
        {
            var rockContext = new RockContext();
            var interactionService = new InteractionService(rockContext);
            var timeZoneInfo = RockDateTime.OrgTimeZoneInfo;
            ResponseModel responseData = new ResponseModel()
            {
                Success=false,
                Message="Invalid Request"
            };

            /**
             * Initialize response objects
             */
            HttpStatusCode statusCode = HttpStatusCode.BadRequest;
            HttpContent httpContent = null;
            HttpResponseMessage response = new HttpResponseMessage() { StatusCode = statusCode, Content = httpContent };

            if (attendanceModel == null)
            {
                return response;
            }

            response.StatusCode = HttpStatusCode.OK;

            var dt = DateTime.Now;
            dt = TimeZoneInfo.ConvertTime(dt, timeZoneInfo);
            Interaction thisInteraction = new Interaction()
            {
                EntityId = attendanceModel.EntityId,
                Operation = attendanceModel.Operation,
                InteractionDateTime = dt,
                PersonAliasId = attendanceModel.PersonAliasId,
                InteractionComponentId = attendanceModel.InteractionComponentId
            };

            interactionService.Add(thisInteraction);

            rockContext.SaveChanges();

            responseData.Message = "Interaction created with Id " + thisInteraction.Id.ToString();
            responseData.Success = true;
            responseData.Data = "{\"Id\":" + thisInteraction.Id.ToString() + "\"}";

            response.Content = new StringContent(JsonConvert.SerializeObject(responseData), Encoding.UTF8, "application/json");

            return response;

        }
    }
}
