using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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
            List<MobileAppSeriesModel> seriesList = new List<MobileAppSeriesModel>();

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

                MobileAppSeriesModel maSeriesModel = MobileAppService.PodcastSeriesToMobileAppSeries( series );
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
                MobileAppSeriesModel maSeriesModel = MobileAppService.PodcastSeriesToMobileAppSeries( series );

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
    }
}
