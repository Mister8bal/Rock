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

    /// <summary>
    /// 
    /// </summary>
    public class AttendanceController : Rock.Rest.ApiControllerBase
    {
        /// <summary>
        /// Use this to log an Attendance Interaction on the CCV Live Interaction Channel 
        /// </summary>
        /// <param name="attendanceModel">The opject containing the attendance data to be logged with the interaction <see cref="church.ccv.CCVRest.CCVLive.Model.AttendanceModel"/>.</param>
        /// <exception cref="System.Web.Http.HttpResponseException"></exception>
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/CCVLive/Attendance")]
        [Authenticate, Secured]
        public HttpResponseMessage Attendance(AttendanceModel attendanceModel)
        {
            var rockContext = new RockContext();
            var interactionService = new InteractionService(rockContext);
            var timeZoneInfo = RockDateTime.OrgTimeZoneInfo;
            var person = GetPerson(attendanceModel.Email, attendanceModel.Name, rockContext);
            
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

            /**
             * From this point on, all responses are valid.
             */ 
            statusCode = HttpStatusCode.OK;

            /**
             * If no person is found, we simply return a 200 response with an appropriate message.
             * The request doesn't need to fail at this point.  Just let the user know that no person 
             * is registered with the provided data. 
             */ 
            if (person == null)
            {
                responseData.Success = false;
                responseData.Message = "We could not find a person with that name and email address.";
                response.StatusCode = statusCode;
                response.Content = new StringContent(JsonConvert.SerializeObject(responseData), Encoding.UTF8, "application/json");
                return response;
            }
    
            response.StatusCode = statusCode;

            var dt = DateTime.Now;
            dt = TimeZoneInfo.ConvertTime(dt, timeZoneInfo);

            /** 
             * Instantiate the interaction
             */ 
            Interaction thisInteraction = new Interaction()
            {
                Operation = attendanceModel.Operation,
                InteractionDateTime = dt,
                PersonAliasId = person.Id,
                InteractionComponentId = attendanceModel.InteractionComponentId,
                InteractionSessionId = attendanceModel.InteractionSessionId
            };

            interactionService.Add(thisInteraction);

            rockContext.SaveChanges();

            responseData.Message = "Interaction created with Id " + thisInteraction.Id.ToString();
            responseData.Success = true;
            responseData.Data = "{\"Id\":" + thisInteraction.Id + "}";

            response.Content = new StringContent(JsonConvert.SerializeObject(responseData), Encoding.UTF8, "application/json");

            return response;

        }

        /// <summary>
        /// Gets first person associated with a specific phone number. If there is more 
        /// than one person, the oldest adult will be returned first. If no adults, the
        /// oldest child.
        /// </summary>
        /// <param name="fromPhone">From phone.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        private static Person GetPerson(string email, string name, RockContext rockContext)
        {

            var personQuery = new PersonService( rockContext );


            var person = personQuery
                .Queryable()
                .Where(e => (e.Email == email) && (e.FirstName == name || e.NickName == name)).FirstOrDefault();
               

            // If we have a person, return it
            if (person != null) return person;

            return null;

        }
    }
}
