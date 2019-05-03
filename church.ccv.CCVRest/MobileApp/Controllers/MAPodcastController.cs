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
        public enum PodcastLatestMessageResponse
        {
            NotSet = -1,

            Success,

            NotAvailable,

            PodcastError
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Podcast/LatestMessage" )]
        [Authenticate, Secured]
        public HttpResponseMessage LatestMessage()
        {
            // This is a little weird, but we need the "Latest Message" which is a very abstract concept.
            MobileAppMessageModel latestMessage = null;

            // Basically, it's the first Series we find that isn't Hidden. Within that, the first Message that isn't Hidden.
            // That is the technical definition of "Latest Message".

            // we will grab the 3 most recent series, because generally speaking that is more than enough to have a public message.
            PodcastUtil.PodcastCategory rootCategory = PodcastUtil.GetPodcastsByCategory( PodcastUtil.WeekendVideos_CategoryId, false, 3 );
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

                // this is terrible performance-wise, but simpler to maintain and read.

                // Convert the series
                MASeriesModel maSeriesModel = MAPodcastService.PodcastSeriesToMobileAppSeries( series );

                // see if it's hidden
                if ( maSeriesModel.Hidden == false )
                {
                    // it isn't, so it is GOING to have the message we want
                    foreach ( MobileAppMessageModel maMessageModel in maSeriesModel.Messages )
                    {
                        if ( maMessageModel.Hidden == false )
                        {
                            // we found our message! Return it.
                            latestMessage = maMessageModel;
                            break;
                        }
                    }

                    // we can break out of the series now because we know we have a latest message
                    break;
                }
            }

            // after all the searching above, if we have a latest message, yay.
            if ( latestMessage != null )
            {
                return Common.Util.GenerateResponse( true, PodcastSeriesResponse.Success.ToString(), latestMessage );
            }
            else
            {
                // this should be extremely uncommon if not impossible, but handle it just in case
                return Common.Util.GenerateResponse( false, PodcastLatestMessageResponse.NotAvailable.ToString(), null );
            }
        }

        [Serializable]
        public enum PodcastToolboxResourcesResponse
        {
            NotSet = -1,

            Success,

            PodcastError
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Podcast/ToolboxResources" )]
        [Authenticate, Secured]
        public HttpResponseMessage ToolboxResources(int primaryAliasId)
        {
            List<ToolboxResourceModel> toolboxResourceList = new List<ToolboxResourceModel>();

            // track the resources we need to get, so we don't go over the limit
            int resourcesRemaining = 4;

            // ultimately, we want 4 resources/messages. However, because some could be private, and we also need to know the index
            // of the messsage within its series (For the Week Number: N feature), we have to take whole series. 
            // Grab the most recent six, which should be more than enough to cover 4 messages.
            PodcastUtil.PodcastCategory rootCategory = PodcastUtil.GetPodcastsByCategory( 470, false, 6, int.MaxValue, primaryAliasId );
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

                // use the resourcesRemaining int to track when we've hit our total number
                List<ToolboxResourceModel> resourceList  = MAPodcastService.PodcastSeriesToToolboxResources( series, ref resourcesRemaining );
                toolboxResourceList.AddRange( resourceList );

                // if parsing this latest series caused us to hit our goal, break.
                if ( resourcesRemaining == 0 )
                {
                    break;
                }
            }

            return Common.Util.GenerateResponse( true, PodcastToolboxResourcesResponse.Success.ToString(), toolboxResourceList );
        }
    }
}
