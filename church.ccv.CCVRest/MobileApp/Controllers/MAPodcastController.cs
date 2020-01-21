using System;
using System.Collections.Generic;
using System.Net.Http;
using church.ccv.CCVRest.MobileApp.Model;
using church.ccv.Podcast;
using Rock.Rest.Filters;

namespace church.ccv.CCVRest.MobileApp
{
    public partial class NewMobileAppController : Rock.Rest.ApiControllerBase
    {
        [Serializable]
        public enum PodcastSeriesResponse
        {
            NotSet = -1,

            Success,

            PodcastError
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Podcast/Series" )]
        [Authenticate, Secured]
        public HttpResponseMessage Series( int numSeries = 12 )
        {
            List<MASeriesModel> seriesList = new List<MASeriesModel>();

            PodcastUtil.PodcastCategory rootCategory = PodcastUtil.GetPodcastsByCategory( PodcastUtil.WeekendVideos_CategoryId, false, numSeries );
            if ( rootCategory == null )
            {
                // if this failed something really bad happened
                return Common.Util.GenerateResponse( false, PodcastSeriesResponse.PodcastError.ToString(), null );
            }

            // iterate over each series
            foreach ( PodcastUtil.IPodcastNode podcastNode in rootCategory.Children )
            {
                // this is safe to cast to a series, because we ask for only Series by passing false to GetPodcastsByCategory                        
                PodcastUtil.PodcastSeries series = podcastNode as PodcastUtil.PodcastSeries;

                MASeriesModel maSeriesModel = MAPodcastService.PodcastSeriesToMobileAppSeries( series );
                seriesList.Add( maSeriesModel );

                // if we're beyond the number of series they wanted, stop.
                if ( seriesList.Count >= numSeries )
                {
                    break;
                }
            }

            return Common.Util.GenerateResponse( true, PodcastSeriesResponse.Success.ToString(), seriesList );
        }

        [Serializable]
        public enum PodcastSingleSeriesResponse
        {
            NotSet = -1,

            Success,

            NotFound
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Podcast/SingleSeries" )]
        public HttpResponseMessage SingleSeries( int seriesId )
        {
            // get the requested series
            PodcastUtil.PodcastSeries series = PodcastUtil.GetSeries( seriesId );
            if ( series == null )
            {
                return Common.Util.GenerateResponse( false, PodcastSingleSeriesResponse.NotFound.ToString(), null );
            }

            MASeriesModel maSeriesModel = MAPodcastService.PodcastSeriesToMobileAppSeries( series );
            return Common.Util.GenerateResponse( true, PodcastSingleSeriesResponse.Success.ToString(), maSeriesModel );
        }

        [Serializable]
        public enum PodcastMessageResponse
        {
            NotSet = -1,

            Success,

            NotFound
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Podcast/Message" )]
        public HttpResponseMessage Message( int messageId )
        {
            // get the requested message
            PodcastUtil.PodcastMessage message = PodcastUtil.GetMessage( messageId );
            if ( message == null )
            {
                return Common.Util.GenerateResponse( false, PodcastMessageResponse.NotFound.ToString(), null );
            }

            // now we need the parent series so we can grab the image
            // dont check for null because the series MUST exist for the message to exist, or something catastrophic happened
            PodcastUtil.PodcastSeries series = PodcastUtil.GetSeries( message.SeriesId );
            string seriesImageUrl = MAPodcastService.GetSeriesImageURL( series );

            MobileAppMessageModel maMessageModel = MAPodcastService.PodcastMessageToMobileAppMessage( message, seriesImageUrl );
            return Common.Util.GenerateResponse( true, PodcastMessageResponse.Success.ToString(), maMessageModel );
        }

        [Serializable]
        public enum PodcastLatestMessageResponse
        {
            NotSet = -1,

            Success,

            NotAvailable
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Podcast/LatestMessage" )]
        [Authenticate, Secured]
        public HttpResponseMessage LatestMessage()
        {
            // The primary reason to call this endpoint is to advertise a banner to the user with a quick link to the latest message.
            // We want to make this Advertisement window server-side so that updates can be made without pushing an app update.

            // get the "latest" message, which should be basically the newest available message in the newest series.
            MobileAppMessageModel latestMessage = MAPodcastService.GetLatestMessage( );

            if ( latestMessage != null )
            {
                MobileAppLatestMessageModel latestMessageWrapper = new MobileAppLatestMessageModel();
                latestMessageWrapper.Message = latestMessage;

                // TODO: Make this window data driven
                // should the banner be displayed?
                // yes, if it's a weekend and within our (hardcoded for now) service hours
                if ( ( DateTime.Now.DayOfWeek == DayOfWeek.Saturday && DateTime.Now.Hour >= 15 && DateTime.Now.Hour <= 20 )  //Saturday from 3pm to 8pm
                  || ( DateTime.Now.DayOfWeek == DayOfWeek.Sunday && DateTime.Now.Hour >= 8 && DateTime.Now.Hour <= 15 ) ) //Sunday from 8am to 3pm
                {
                    latestMessageWrapper.ShouldDisplayBanner = true;
                }

                return Common.Util.GenerateResponse( true, PodcastSeriesResponse.Success.ToString(), latestMessageWrapper );
            }
            else
            {
                // this should be extremely uncommon if not impossible, but handle it just in case
                return Common.Util.GenerateResponse( false, PodcastLatestMessageResponse.NotAvailable.ToString(), null );
            }
        }

        [Serializable]
        public enum PodcastUserNotesResponse
        {
            NotSet = -1,

            Success,

            PodcastError
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Podcast/UserNotes" )]
        [Authenticate, Secured]
        public HttpResponseMessage UserNotes( int primaryAliasId )
        {
            // Eventually, this will take the person's primaryAliasId, and use the CloudUserNote service to get a UserNoteSummary
            // for each message they've taken notes in.

            // Until we have that built, we'll just provide a list of every message they could possibly have a usernote in, which
            // is any message from when Mobile App 2.0 launched until now. MA 2.0 launched in September 2015, so that would be the series
            // Messy Grace, Id 151.
            const int LastValidSeriesId = 151;

            List<MASeriesModel> seriesList = new List<MASeriesModel>();

            // for now, get all series since the beginning of time
            PodcastUtil.PodcastCategory rootCategory = PodcastUtil.GetPodcastsByCategory( PodcastUtil.WeekendVideos_CategoryId, false, int.MaxValue - 1 );
            if ( rootCategory == null )
            {
                // if this failed something really bad happened
                return Common.Util.GenerateResponse( false, PodcastUserNotesResponse.PodcastError.ToString(), null );
            }

            // convert the series / messages into UserNoteSummaryModels
            List<MAUserNoteSummaryModel> userNoteSummaryList = new List<MAUserNoteSummaryModel>();

            foreach ( PodcastUtil.IPodcastNode podcastNode in rootCategory.Children )
            {
                // this is safe to cast to a series, because we ask for only Series by passing false to GetPodcastsByCategory                        
                PodcastUtil.PodcastSeries series = podcastNode as PodcastUtil.PodcastSeries;

                MASeriesModel maSeriesModel = MAPodcastService.PodcastSeriesToMobileAppSeries( series );

                foreach ( MobileAppMessageModel message in maSeriesModel.Messages )
                {
                    MAUserNoteSummaryModel summary = new MAUserNoteSummaryModel();
                    summary.SeriesName = series.Name;
                    summary.SeriesImageURL = maSeriesModel.ImageURL;
                    summary.SeriesDateRange = maSeriesModel.DateRange;
                    summary.MessageName = message.Name;
                    summary.MessageNoteURL = message.NoteURL;
                    summary.MessageSpeaker = message.Speaker;

                    userNoteSummaryList.Add( summary );
                }

                // if we go past the oldest valid series, stop.
                if ( maSeriesModel.Id < LastValidSeriesId )
                {
                    break;
                }
            }

            return Common.Util.GenerateResponse( true, PodcastUserNotesResponse.Success.ToString(), userNoteSummaryList );
        }
    }
}
