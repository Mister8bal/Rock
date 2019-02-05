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

    static HttpClient Client = new HttpClient();
    int GroupId = 2557652;
    
    // Pull the Eventbrite OAuth Token from global attributes
    string EVB_TOKEN = GlobalAttributesCache.Read().GetValue("EventBritePersonalOAuthToken").ToString();

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
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", EVB_TOKEN);

        response.ContentType = "text/plain";

        if ( request.HttpMethod != "POST" || requestData == null)
        {
            response.Write( "Invalid request type. Please use POST." );

            response.StatusCode = HttpStatusCode.Forbidden.ConvertToInt();

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
            case "order.refunded": HandleEventBriteOrderRefunded(requestData);//handle refund order 
                break;
            case "attendee.updated": HandleEventBriteAttendeeUpdate(requestData); //handle attendee updated
                break;
            case "order.updated": HandleEventBriteOrderUpdated(requestData);
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

    /// <summary>
    /// Sumbit a request to the Eventbrite API to get specific order data
    /// </summary>
    /// <param name="path">The api endpoint to use in the request.  This should
    /// be provided by the request to the webhook</param>
    /// <returns>A valid Instantiated EventBriteOrder<see cref="EventBriteOrder"/></returns>
    static async Task<EventBriteOrder> GetOrderAsync(string path)
    {
        EventBriteOrder order = null;
        HttpResponseMessage response = await Client.GetAsync(path);
        if (response.IsSuccessStatusCode)
        {
            order = await response.Content.ReadAsAsync<EventBriteOrder>();
        }
        return order;
    }
    
    /// <summary>
    /// Submit a request to the EventBrite Api for individual ticket / Attendee 
    /// Info.  Eventbrite refers to tickets as attendees.  The terms are used
    /// interchangably in this code.
    /// </summary>
    /// <param name="path">A valid Sventbrite API Endpoint</param>
    /// <returns> An instantiated Eventbrite Ticket Object<see cref="Ticket"/></returns>
    static async Task<Ticket> GetTicketAsync(string path)
    {
        Ticket ticket = null;
        HttpResponseMessage response = await Client.GetAsync(path);
        if (response.IsSuccessStatusCode)
        {
            ticket = await response.Content.ReadAsAsync<Ticket>();
        }
        return ticket;
    }
    
    /// <summary>
    /// Handles any webhook request with the action "order.placed"  
    /// Uses the GetOrderAsync method to retreive the data from the
    /// Eventbrite API:<see cref="GetOrderAsync(string)"/>
    /// </summary>
    /// <param name="Endpoint">A valid Eventbrite API endpoint url</param>
    /// <returns>Async Task</returns>
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
    
    /// <summary>
    /// Handles any webhook request with the action "barcode.checked_in"  
    /// Uses the GetTicketAsync method to retreive the data from the.  
    /// Sets the "Attended" group attribute to true for any returned Group
    /// Member.
    /// Eventbrite API:<see cref="GetTicketAsync(string)"/>
    /// </summary>
    /// <param name="Endpoint">A valid Eventbrite API endpoint url</param>
    /// <returns>Async Task</returns>
    public async Task HandleEventBriteCheckedIn(string Endpoint)
    {

        Ticket ticket = await GetTicketAsync(Endpoint + "?expand=attendees");
        RockContext rockContext = new RockContext();

        if(ticket.profile == null)
        {
            return;
        }

        GroupMember primaryGroupMember = GetGroupMemberFromEventBriteTicket(ticket);

        primaryGroupMember.LoadAttributes();

        primaryGroupMember.SetAttributeValue("Attended", "Yes");

        primaryGroupMember.SaveAttributeValues(rockContext);

    }

    /// <summary>
    /// Handles any webhook request with the action "order.refunded"  
    /// Uses the GetOrderAsync method to retreive the data from Eventbrite.  
    /// If the user data provided in the Webhook request includes a
    /// user that matches a vaild group member, that group member
    /// will be removed.
    /// <see cref="GetOrderAsync(string)"/>
    /// </summary>
    /// <param name="request">A valid EventBriteRequest object.</param>
    /// <returns>Async Task</returns>
    private async Task HandleEventBriteOrderRefunded(EventBriteRequest request)
    {
        EventBriteOrder orderData = await GetOrderAsync(request.api_url + "?expand=attendees");
        RockContext rockContext = new RockContext();

        if(orderData.attendees.Count() <= 0)
        {
            return;
        }

        foreach(Ticket ticket in orderData.attendees)
        {
            GroupMember groupMember = GetGroupMemberFromEventBriteTicket(ticket);
            RemoveGroupMemberFromGroup(groupMember, rockContext);
        }

    }
    
    /// <summary>
    /// Handles any webhook request with the action "order.updated"  
    /// Uses the GetOrderAsync method to retreive the order data from Eventbrite.  
    /// If the order contains attendees, we loop through each attendee (ticket)
    /// and check to see if it shows as cancelled or refunded.  Removes
    /// the associated Group member from the group.
    /// <see cref="GetOrderAsync(string)"/>
    /// </summary>
    /// <param name="request">A valid EventBriteRequst object</param>
    /// <returns>Async Task</returns>
    private async Task HandleEventBriteOrderUpdated(EventBriteRequest request)
    {

        var rockContext = new RockContext();

        EventBriteOrder orderData = await GetOrderAsync(request.api_url + "?expand=attendees");

        if(orderData.attendees.Count() <= 0)
        {
            return;
        }


        foreach (Ticket ticket in orderData.attendees)
        {
            GroupMember groupMember = GetGroupMemberFromEventBriteTicket(ticket);
            if(ticket.cancelled || ticket.refunded)
            {
                RemoveGroupMemberFromGroup(groupMember, rockContext);
            }

        }


    }
    
    /// <summary>
    /// Handles any webhook request with the action "attendee.updated"  
    /// Uses the GetTicketAsync method to retreive the attendee data from Eventbrite.  
    /// Performs some action on an associated group member, based on 
    /// the status attribute of the ticket(attendee) object.
    /// Uses GetTicketAsync<see cref="GetTicketAsync(string)"/>
    /// </summary>
    /// <param name="request">A valid EventBriteRequst object</param>
    /// <returns>Async Task</returns>
    private async Task HandleEventBriteAttendeeUpdate(EventBriteRequest request)
    {
        Ticket ticket = await GetTicketAsync(request.api_url);
        var rockContext = new RockContext();

        if(ticket == null)
        {
            return;
        }

        GroupMember groupMember = null;
        groupMember = GetGroupMemberFromEventBriteTicket(ticket);

        if (groupMember == null)
        {
            return;
        }

        switch (ticket.status) {

            case "Not Attending":
                RemoveGroupMemberFromGroup(groupMember, rockContext);
                break;

            case "Deleted":
                RemoveGroupMemberFromGroup(groupMember, rockContext);
                break;

            default:
                break;
        }


    }
    
    /// <summary>
    /// Attempts to locate a GroupMember using data from a 
    /// supplied Eventbrite Ticket object.
    /// </summary>
    /// <param name="ticket">A valid Ticket object</param>
    /// <returns>A valid Group member object or null if not found<see cref="GroupMember"/></returns>
    private GroupMember GetGroupMemberFromEventBriteTicket(Ticket ticket)
    {
        GroupMember groupMember = null;

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

        groupMember = groupMemberService.GetByGroupIdAndPersonId(requestedGroup.Id, person.Id).First();

        return groupMember;
    }
    
    /// <summary>
    /// Attempts to register a person in a group.  
    /// </summary>
    /// <param name="attendeeProfile">A valid EventBrite attendee Profile object</param>
    /// <param name="requestedGroupId">The id of the group to register the user in if found.</param>
    /// <returns>Boolean: True if registration was successfull.</returns>
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
            if (matches.Count() >= 1)
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

            // now, it's time to either add them to the group,
            // (Or nothing if there's no problem but they're already in the group)
            GroupMember primaryGroupMember = PersonToGroupMember(rockContext, person, requestedGroup);


            // try to add them to the group (would only fail if the're already in it)
            TryAddGroupMemberToGroup(rockContext, primaryGroupMember, requestedGroup);

            // if we mae it here, all is good!
            success = true;
        }

        return success;
    }
    /// <summary>
    /// Removes a provided group member from the associated group.
    /// </summary>
    /// <param name="gm">A valid GroupMember object<see cref="GroupMember"/></param>
    /// <param name="rockContext">The Rock context to use</param>
    /// <returns>Boolean: True on success.</returns>
    private bool RemoveGroupMemberFromGroup(GroupMember gm, RockContext rockContext)
    {
        GroupMemberService groupMemberService = new GroupMemberService(rockContext);

        string errorMessage;

        if(groupMemberService.CanDelete(gm, out errorMessage)){
            groupMemberService.Delete(gm);
        }

        rockContext.SaveChanges();

        return false;
    }
    
    /// <summary>
    /// Builds a GroupMember from a vaid Rock Person.  This method simply instantiates
    /// the object.  In order to persist the data, save must be called on the
    /// provided rock context.
    /// </summary>
    /// <param name="rockContext">The rock context to use.</param>
    /// <param name="person">A valid instantiated Person object<see cref="Rock.Model.Person"/></param>
    /// <param name="group">A valid group object representing the group to add the the Person to.</param>
    /// <returns></returns>
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
    public bool cancelled { get; set; }
    public bool refunded { get; set; }
    public string status;

}

public class AttendeeProfile
{
    public string first_name { get; set; }
    public string last_name { get; set; }
    public string email { get; set; }
    public string home_phone { get; set; }

}


