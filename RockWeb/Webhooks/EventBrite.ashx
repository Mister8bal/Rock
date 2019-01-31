<%@ WebHandler Language="C#" Class="EventBrite" %>
// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.IO;
using System.Web;
using System.Threading;
using System.Collections.Generic;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using System.Data.Entity;
using church.ccv.MobileApp.Models;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using church.ccv.Actions;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
/// EventBrite Webhook Handler
/// </summary>
public class EventBrite : IHttpAsyncHandler
{
    /// <summary>
    /// Begins the process request.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="cb">The cb.</param>
    /// <param name="extraData">The extra data.</param>
    /// <returns></returns>

    public IAsyncResult BeginProcessRequest( HttpContext context, AsyncCallback cb, Object extraData )
    {
        EventBriteReponseAsync eventbriteAsync = new EventBriteReponseAsync( cb, context, extraData );
        eventbriteAsync.StartAsyncWork();
        return eventbriteAsync;
    }

    /// <summary>
    /// Provides an asynchronous process End method when the process ends.
    /// </summary>
    /// <param name="result">An <see cref="T:System.IAsyncResult" /> that contains information about the status of the process.</param>
    public void EndProcessRequest( IAsyncResult result )
    {
    }

    /// <summary>
    /// Enables processing of HTTP Web requests by a custom HttpHandler that implements the <see cref="T:System.Web.IHttpHandler" /> interface.
    /// </summary>
    /// <param name="context">An <see cref="T:System.Web.HttpContext" /> object that provides references to the intrinsic server objects (for example, Request, Response, Session, and Server) used to service HTTP requests.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void ProcessRequest( HttpContext context )
    {
        throw new InvalidOperationException();
    }

    /// <summary>
    /// Gets a value indicating whether another request can use the <see cref="T:System.Web.IHttpHandler" /> instance.
    /// </summary>
    public bool IsReusable
    {
        get
        {
            return false;
        }
    }
}

/// <summary>
/// Async Result for text-to-workflow
/// </summary>
class EventBriteReponseAsync : IAsyncResult
{
    private bool _completed;
    private Object _state;
    private AsyncCallback _callback;
    private HttpContext _context;

    bool IAsyncResult.IsCompleted { get { return _completed; } }
    WaitHandle IAsyncResult.AsyncWaitHandle { get { return null; } }
    Object IAsyncResult.AsyncState { get { return _state; } }
    bool IAsyncResult.CompletedSynchronously { get { return false; } }

    static HttpClient client = new HttpClient();
    int GroupId = 2500748;


    /// <summary>
    /// Initializes a new instance of the <see cref="ReponseAsync"/> class.
    /// </summary>
    /// <param name="callback"></param>
    /// <param name="context"></param>
    /// <param name="state"></param>
    /// <returns>true if the asynchronous operation completed synchronously; otherwise, false.</returns>
    public EventBriteReponseAsync( AsyncCallback callback, HttpContext context, Object state )
    {
        _callback = callback;
        _context = context;
        _state = state;
        _completed = false;
    }

    /// <summary>
    /// Starts the asynchronous work.
    /// </summary>
    public void StartAsyncWork()
    {
        ThreadPool.QueueUserWorkItem( new WaitCallback( StartAsyncTask ), null );
    }

    /// <summary>
    /// Starts the asynchronous task.  In our case, grab the EventBrite request data and call 
    /// the necessary function based on the action parameter sent with the webhook request.
    /// </summary>
    /// <param name="workItemState">State of the work item.</param>
    private void StartAsyncTask( Object workItemState )
    {
        var request = _context.Request;
        var response = _context.Response;
        var requestData = GetRequestData(request);

        //Set the authorization token for Eventbrite API calls. 
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "");

        response.ContentType = "text/plain";

        if ( request.HttpMethod != "POST" || requestData == null)
        {
            response.Write( "Invalid request type. Please use POST." );

            _completed = true;

            _callback( this );

            return;
        }

        /**
         * Call appropriate handler method based on the 
         * action sent by the webhook request. 
         */
        switch (requestData.config.action)
        {
            case "order.placed": HandleEventBriteOrderPlaced(requestData.api_url);
                break;
            case "barcode.checked_in": HandleEventBriteCheckedIn(requestData.api_url);
                break;
            default:
                break;
        }

        _completed = true;

        _callback( this );

    }
    
    /// <summary>
    /// Parse and return the request body data as an EventBriteRequest object
    /// </summary>
    /// <param name="request">A valid HttpRequest object</param>
    /// <returns>Instantiate EventBriteRequest object<see cref="EventBriteRequest"/></returns>
    private EventBriteRequest GetRequestData(HttpRequest request)
    {
        EventBriteRequest requestData;
        ;
        using (Stream receiveStream = request.InputStream)
        {
            using (StreamReader readStream = new StreamReader(receiveStream))
            {
                requestData = JsonConvert.DeserializeObject<EventBriteRequest>(readStream.ReadToEnd());
            }
        }
        return requestData;
    }


    static async Task<EventBriteOrder> GetOrderAsync(string path)
    {
        EventBriteOrder order = null;
        HttpResponseMessage response = await client.GetAsync(path);
        if (response.IsSuccessStatusCode)
        {
            order = await response.Content.ReadAsAsync<EventBriteOrder>();
        }
        return order;
    }

    static async Task<Ticket> GetTicketAsync(string path)
    {
        Ticket ticket = null;
        HttpResponseMessage response = await client.GetAsync(path);
        if (response.IsSuccessStatusCode)
        {
            ticket = await response.Content.ReadAsAsync<Ticket>();
        }
        return ticket;
    }

    public async Task HandleEventBriteOrderPlaced(string Endpoint)
    {

        EventBriteOrder orderData = await GetOrderAsync(Endpoint + "?expand=attendees");

        if(orderData.attendees.Count() <= 0)
        {
            return;
        }

        foreach(Ticket ticket in orderData.attendees)
        {
            RegisterPersonInGroup(ticket.profile, GroupId);
        }
    }

    public async Task HandleEventBriteCheckedIn(string Endpoint)
    {

        Ticket ticket = await GetTicketAsync(Endpoint + "?expand=attendees");

        if(ticket.profile == null)
        {
            return;
        }

        var rockContext = new RockContext();
        var personService = new PersonService(rockContext);
        var groupService = new GroupService(rockContext);
        var groupMemberService = new GroupMemberService(rockContext);

        Person person = null;

        Group requestedGroup = groupService.Get(GroupId);
        //This is where we need to find the person, pull the group memeber and update the attribute.
        var matches = personService.GetByMatch(ticket.profile.first_name.Trim(), ticket.profile.last_name.Trim(), ticket.profile.email.Trim());
        if (matches.Count() == 1)
        {
            person = matches.First();
        }

        GroupMember primaryGroupMember = groupMemberService.GetByGroupIdAndPersonId(requestedGroup.Id, person.Id).First();

        primaryGroupMember.LoadAttributes();

        primaryGroupMember.SetAttributeValue("Attended", "Yes");

        primaryGroupMember.SaveAttributeValues(rockContext);

    }

    public static void SetGroupMemberAttendance(Person person)
    {
        if(person == null)
        {
            return;
        }
    }

    public static bool RegisterPersonInGroup(AttendeeProfile attendeeProfile, int requestedGroupId)
    {
        bool success = false;

        // setup all variables we'll need
        var rockContext = new RockContext();
        var personService = new PersonService(rockContext);
        var groupService = new GroupService(rockContext);

        DefinedValueCache connectionStatusPending = DefinedValueCache.Read(Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT);
        DefinedValueCache recordStatusPending = DefinedValueCache.Read(Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING);

        Person person = null;
        Group family = null;

        // setup history tracking
        var changes = new List<string>();
        var familyChanges = new List<string>();

        // first, get the group the person wants to join
        Group requestedGroup = groupService.Get(requestedGroupId);
        if (requestedGroup != null)
        {
            // Try to find person by name/email 
            var matches = personService.GetByMatch(attendeeProfile.first_name.Trim(), attendeeProfile.last_name.Trim(), attendeeProfile.email.Trim());
            if (matches.Count() == 1)
            {
                person = matches.First();
            }

            // Check to see if this is a new person
            if (person == null)
            {
                // If so, create the person and family record for the new person
                person = new Person();
                person.FirstName = attendeeProfile.first_name.Trim();
                person.LastName = attendeeProfile.last_name.Trim();
                person.Email = attendeeProfile.email.Trim();
                person.IsEmailActive = true;
                person.EmailPreference = EmailPreference.EmailAllowed;
                person.RecordTypeValueId = DefinedValueCache.Read(Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid()).Id;
                person.ConnectionStatusValueId = connectionStatusPending.Id;
                person.RecordStatusValueId = recordStatusPending.Id;
                person.Gender = Gender.Unknown;

                family = PersonService.SaveNewPerson(person, rockContext, requestedGroup.CampusId, false);
            }


            // if provided, store their phone number
            if (string.IsNullOrWhiteSpace(attendeeProfile.home_phone) == false)
            {
                DefinedValueCache mobilePhoneType = DefinedValueCache.Read(Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE);
                person.UpdatePhoneNumber(mobilePhoneType.Id, PhoneNumber.DefaultCountryCode(), attendeeProfile.home_phone, null, null, rockContext);
            }

            // Save all changes
            rockContext.SaveChanges();

            // now, it's time to either add them to the group, or kick off the Alert Re-Route workflow
            // (Or nothing if there's no problem but they're already in the group)
            GroupMember primaryGroupMember = PersonToGroupMember(rockContext, person, requestedGroup);


            // try to add them to the group (would only fail if the're already in it)
            TryAddGroupMemberToGroup(rockContext, primaryGroupMember, requestedGroup);

            // if we mae it here, all is good!
            success = true;
        }

        return success;
    }

    private static GroupMember PersonToGroupMember(RockContext rockContext, Person person, Group group)
    {
        // puts a person into a group member object, so that we can pass it to a workflow
        GroupMember newGroupMember = new GroupMember();
        newGroupMember.PersonId = person.Id;
        newGroupMember.GroupRoleId = group.GroupType.DefaultGroupRole.Id;
        newGroupMember.GroupMemberStatus = GroupMemberStatus.Active;
        newGroupMember.GroupId = group.Id;

        return newGroupMember;
    }

    /// <summary>
    /// Adds the group member to the group if they aren't already in it
    /// </summary>
    private static void TryAddGroupMemberToGroup(RockContext rockContext, GroupMember newGroupMember, Group group)
    {
        if (!group.Members.Any(m =>
            m.PersonId == newGroupMember.PersonId &&
            m.GroupRoleId == group.GroupType.DefaultGroupRole.Id))
        {
            var groupMemberService = new GroupMemberService(rockContext);
            groupMemberService.Add(newGroupMember);

            rockContext.SaveChanges();
        }
    }

    private static void GetGroupMember(RockContext rockContext, GroupMember groupMember, Group group)
    {
        var foundMembers = group.Members.Any(m =>
            m.PersonId == groupMember.PersonId &&
            m.GroupRoleId == group.GroupType.DefaultGroupRole.Id);

    }
}

public class EventBriteRequestConfig
{
    public string action { get; set; }
    public string user_id { get; set; }
    public string endpoint { get; set; }
    public string webhook_id { get; set; }
}

public class EventBriteRequest
{
    public EventBriteRequestConfig config { get; set; }
    public string api_url { get; set; }
}

public class EventBriteOrder
{
    public string first_name { get; set; }
    public string last_name { get; set; }
    public string name { get; set; }
    public string status { get; set; }
    public string event_id { get; set; }
    public IList<Ticket> attendees;
}

public class Ticket
{

    public string team { get; set; }
    public string resource_uri { get; set; }
    public int quantity { get; set; }
    public AttendeeProfile profile { get; set; }
    public bool checked_in { get; set; }

}

public class AttendeeProfile
{
    public string first_name { get; set; }
    public string last_name { get; set; }
    public string email { get; set; }
    public string home_phone { get; set; }

}


