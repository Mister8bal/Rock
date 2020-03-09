<%@ WebHandler Language="C#" Class="HubSpot" %>

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Web;
using church.ccv.CCVCore.HubSpot.Util;
using Rock;

/// <summary>
/// HubSpot Webhook
/// </summary>
public class HubSpot : IHttpAsyncHandler
{
    /// <summary>
    /// Begins the process request
    /// </summary>
    /// <param name="context"></param>
    /// <param name="callback"></param>
    /// <param name="extraData"></param>
    /// <returns></returns>
    public IAsyncResult BeginProcessRequest( HttpContext context, AsyncCallback callback, Object extraData )
    {
        HubSpotResponseAsync hubSpotResponseAsync = new HubSpotResponseAsync( context, callback, extraData );
        hubSpotResponseAsync.StartAsyncWork();
        return hubSpotResponseAsync;
    }

    /// <summary>
    /// Provides an asynchronous process End method when the process ends
    /// </summary>
    /// <param name="result"></param>
    public void EndProcessRequest( IAsyncResult result )
    {
    }

    /// <summary>
    /// Enables process of HTTP Web requests by a custom HttpHandler that implements the <see cref="T:System.Web.IHttpHandler" /> interface.
    /// </summary>
    /// <param name="context"></param>
    public void ProcessRequest( HttpContext context )
    {
        throw new InvalidOperationException();
    }

    /// <summary>
    /// Gets a value indicating whether another request can use the <see cref="T:System.Web.IHttpHandler" /> instance
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
/// Async Result for HubSpot
/// </summary>
class HubSpotResponseAsync : IAsyncResult
{
    private bool _completed;
    private Object _state;
    private AsyncCallback _callback;
    private HttpContext _context;

    bool IAsyncResult.IsCompleted { get { return _completed;  } }
    WaitHandle IAsyncResult.AsyncWaitHandle { get { return null; } }
    Object IAsyncResult.AsyncState {  get { return _state; } }
    bool IAsyncResult.CompletedSynchronously {  get { return false; } }

    /// <summary>
    /// Initializes a new instance of the <see cref="HubSpotResponseAsync" /> class
    /// </summary>
    /// <param name="context"></param>
    /// <param name="callback"></param>
    /// <param name="state"></param>
    /// <returns>true if the asynchronous operation completed synchronously; otherwise false</returns>
    public HubSpotResponseAsync( HttpContext context, AsyncCallback callback, Object state )
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
    /// <param name="workItemState"></param>
    private async void StartAsyncTask( Object workItemState )
    {
        var response = _context.Response;
        response.ContentType = "text/plain";
        var request = _context.Request;

        // ensure valid request type
        if ( request.HttpMethod != "POST" )
        {
            response.Write( "Invalid request type. Please use POST" );
            _completed = true;
            _callback( this );

            return;
        }

        // convert input stream to string
        string requestBody = "";
        using ( StreamReader streamReader = new StreamReader( request.InputStream, Encoding.UTF8 ) )
        {
            requestBody = streamReader.ReadToEnd();
        }

        // ensure we have events to process
        if ( requestBody.IsNullOrWhiteSpace() )
        {
            response.Write( "No events to process" );
            _completed = true;
            _callback( this );

            return;
        }

        // ensure the request came from HubSpot
        string xHubSpotSignature = request.Headers["X-HubSpot-Signature"];
        if ( xHubSpotSignature.IsNullOrWhiteSpace() )
        {
            response.Write( "Missing HubSpot Signature" );
            _completed = true;
            _callback( this );

            return;
        }
        bool validRequest = HubSpotContactService.ValidateRequest( xHubSpotSignature, requestBody );
        if ( !validRequest )
        {
            response.Write( "Invalid HubSpot Signature" );
            _completed = true;
            _callback( this );

            return;
        }

        // process the events
        try
        {
            await HubSpotContactService.ProcessEvents( requestBody );
            response.Write( "Event(s) processing completed" );
        }
        catch ( Exception e )
        {
            response.Write( "Processing failed with errors" );
            Debug.WriteLine( "Processing failed with error:" );
            Debug.WriteLine( e.Message );
        }

        _completed = true;
        _callback( this );
    }
}