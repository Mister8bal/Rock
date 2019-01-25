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
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Web;
using System.Web.Http;
using System.Threading;

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
    /// Starts the asynchronous task.
    /// </summary>
    /// <param name="workItemState">State of the work item.</param>
    private void StartAsyncTask( Object workItemState )
    {
        var request = _context.Request;
        var response = _context.Response;
        var requestData = GetRequestData(request);

        var api_url = request["api_url"];

        response.ContentType = "text/plain";

        if ( request.HttpMethod != "POST" )
        {
            response.Write( "Invalid request type. Please use POST." );

            _completed = true;

            _callback( this );
        }

    }

    private string GetRequestData(HttpRequest request)
    {
        string documentContents;
        using (Stream receiveStream = request.InputStream)
        {
            using (StreamReader readStream = new StreamReader(receiveStream))
            {
                documentContents = readStream.ReadToEnd();
            }
        }
        return documentContents;
    }
}