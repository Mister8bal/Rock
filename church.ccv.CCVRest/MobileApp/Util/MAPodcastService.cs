using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using church.ccv.CCVRest.Common.Model;
using church.ccv.CCVRest.MobileApp.Model;
using church.ccv.Podcast;
using Rock;
using Rock.Web.Cache;

namespace church.ccv.CCVRest.MobileApp
{
    public class MAPodcastService
    {
        public static MASeriesModel PodcastSeriesToMobileAppSeries( PodcastUtil.PodcastSeries series )
        {
            MASeriesModel maSeriesModel = new MASeriesModel();

            maSeriesModel.Id = series.Id;
            maSeriesModel.Name = series.Name;
            maSeriesModel.Description = series.Description;

            // parse and setup the date range for the series
            string dateRangeStr = string.Empty;
            series.Attributes.TryGetValue( "DateRange", out dateRangeStr );
            if ( string.IsNullOrWhiteSpace( dateRangeStr ) == false )
            {
                string[] dateRanges = dateRangeStr.Split( ',' );
                string startDate = DateTime.Parse( dateRanges[0] ).ToShortDateString();
                string endDate = DateTime.Parse( dateRanges[1] ).ToShortDateString();

                maSeriesModel.DateRange = startDate + " - " + endDate;
            }

            // set the image
            maSeriesModel.ImageURL = GetSeriesImageURL( series );

            // after we process messages, we'll use this value for deciding whether the Series is hidden.
            bool allMessagesHidden = true;

            // Now generate each message of the series
            maSeriesModel.Messages = new List<MobileAppMessageModel>();
            foreach ( PodcastUtil.PodcastMessage message in series.Messages )
            {
                MobileAppMessageModel maMessageModel = PodcastMessageToMobileAppMessage( message, maSeriesModel.ImageURL );

                // if we find a message that isn't hidden, flag that so we know the series doesn't HAVE to be hidden
                // (it still could be the series itself is flagged hidden)
                if ( maMessageModel.Hidden == false )
                {
                    allMessagesHidden = false;
                }

                maSeriesModel.Messages.Add( maMessageModel );
            }

            // Finally, let's see if the series should be flagged as Hidden.
            // It should be hidden if all messages are hidden OR the seriesActive flag is false
            bool isSeriesActive = series.Attributes["Active"] == "True" ? true : false;

            if ( allMessagesHidden || isSeriesActive == false )
            {
                maSeriesModel.Hidden = true;
            }

            return maSeriesModel;
        }

        public static async Task<List<ToolboxResourceModel>> PodcastSeriesToToolboxResources( PodcastUtil.PodcastSeries series, int resourcesRemaining )
        {
            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

            // for toolbox resources, we don't store the series hierarhcy--each message is its own item,
            // so a single series could spawn multiple resource models
            List<ToolboxResourceModel> toolboxResources = new List<ToolboxResourceModel>();

            // use an i iterator so we can get the "WeekNumber" more easily
            int i = 0;
            for ( i = 0; i < series.Messages.Count; i++ )
            {
                PodcastUtil.PodcastMessage message = series.Messages[i];

                ToolboxResourceModel resource = new ToolboxResourceModel();

                // (because this is a new attrib, and not all messages have it, check for null. default to TRUE since that's what thye'll want the majority of the time.)
                // This is totally confusing, but the KEY is called "Active", however, its name in Rock is "Approved". So confusing. My mistake. Ugh.
                bool messageActive = true;
                if ( message.Attributes.ContainsKey( "Active" ) )
                {
                    messageActive = bool.Parse( message.Attributes["Active"] );
                }

                if ( messageActive && message.Date <= RockDateTime.Now )
                {
                    resource.SeriesId = series.Id;
                    resource.SeriesName = series.Name;
                    resource.MessageId = message.Id;
                    resource.MessageName = message.Name;

                    if ( string.IsNullOrWhiteSpace( series.Attributes["Image_16_9"] ) == false )
                    {
                        resource.SeriesImageURL = publicAppRoot + "GetImage.ashx?Guid=" + series.Attributes["Image_16_9"] + "&width=825";
                    }

                    // Get the most recent sunday date
                    if ( message.Date.HasValue )
                    {
                        resource.WeekendDate = message.Date.Value.StartOfWeek( DayOfWeek.Sunday );
                    }

                    resource.WeekNumber = series.Messages.Count - i;

                    string wistiaId = message.Attributes["WistiaId"];
                    if ( string.IsNullOrWhiteSpace( wistiaId ) == false )
                    {
                        // fetch the actual media for this asset, and get a direct URL to its video
                        await Common.Util.GetWistiaMedia( wistiaId,
                            delegate ( HttpStatusCode statusCode, WistiaMedia media )
                            {
                                // we check for null media in case something changed on the WistiaMedia json object.
                                // Since we can't control what they send, it's better for this one video to fail
                                // rather than the whole endpoint.
                                if ( statusCode == HttpStatusCode.OK && media != null )
                                {
                                    resource.VideoURL = Common.Util.GetWistiaAssetMpeg4URL( media, WistiaAsset.IPhoneVideoFile );
                                }
                            } );
                    }

                    string discussionGuideUrlValue = null;
                    message.Attributes.TryGetValue( "DiscussionGuideUrl", out discussionGuideUrlValue );
                    if ( string.IsNullOrWhiteSpace( discussionGuideUrlValue ) == false )
                    {
                        resource.DiscussionGuideURL = discussionGuideUrlValue;
                    }

                    toolboxResources.Add( resource );

                    // stop if we've reached our limit
                    resourcesRemaining--;
                    if ( resourcesRemaining == 0 )
                    {
                        break;
                    }
                }
            }

            return toolboxResources;
        }

        public static MobileAppMessageModel PodcastMessageToMobileAppMessage( PodcastUtil.PodcastMessage message, string parentSeriesImageURL )
        {
            MobileAppMessageModel maMessageModel = new MobileAppMessageModel();

            // (because this is a new attrib, and not all messages have it, check for null. default to TRUE since that's what thye'll want the majority of the time.)
            // This is totally confusing, but the KEY is called "Active", however, its name in Rock is "Approved". So confusing. My mistake. Ugh.
            bool messageActive = true;
            if ( message.Attributes.ContainsKey( "Active" ) )
            {
                messageActive = bool.Parse( message.Attributes["Active"] );
            }

            // if the message doesn't start yet, or hasn't been approved, set it to private.
            if ( message.Date > RockDateTime.Now || messageActive == false )
            {
                maMessageModel.Hidden = true;
            }

            maMessageModel.Id = message.Id;
            maMessageModel.Name = message.Name;
            maMessageModel.Speaker = message.Attributes["Speaker"];
            maMessageModel.Date = message.Date.Value;
            maMessageModel.ImageURL = parentSeriesImageURL;

            string noteUrlValue = message.Attributes["NoteUrl"];
            if ( string.IsNullOrWhiteSpace( noteUrlValue ) == false )
            {
                maMessageModel.NoteURL = noteUrlValue;
            }

            string watchUrlValue = message.Attributes["WatchUrl"];
            if ( string.IsNullOrWhiteSpace( watchUrlValue ) == false )
            {
                maMessageModel.VideoURL = watchUrlValue;
            }

            string resourcesValue = null;
            message.Attributes.TryGetValue( "ResourcesMobileApp", out resourcesValue );
            if ( string.IsNullOrWhiteSpace( resourcesValue ) == false )
            {
                maMessageModel.ResourcesHTML = resourcesValue;
            }

            string discussionGuideUrlValue = null;
            message.Attributes.TryGetValue( "DiscussionGuideUrl", out discussionGuideUrlValue );
            if ( string.IsNullOrWhiteSpace( discussionGuideUrlValue ) == false )
            {
                maMessageModel.DiscussionGuideURL = discussionGuideUrlValue;
            }

            return maMessageModel;
        }

        public static string GetSeriesImageURL( PodcastUtil.PodcastSeries series )
        {
            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();
            if ( string.IsNullOrWhiteSpace( series.Attributes["Image_16_9"] ) == false )
            {
                return publicAppRoot + "GetImage.ashx?Guid=" + series.Attributes["Image_16_9"] + "&width=1200";
            }

            return string.Empty;
        }

        public static MobileAppMessageModel GetLatestMessage()
        {
            // This is a little weird, but we need the "Latest Message" which is a very abstract concept.
            MobileAppMessageModel latestMessage = null;

            // Basically, it's the first Series we find that isn't Hidden. Within that, the first Message that isn't Hidden.
            // That is the technical definition of "Latest Message".

            // we will grab the 3 most recent series, because generally speaking that is more than enough to have a public message.
            PodcastUtil.PodcastCategory rootCategory = PodcastUtil.GetPodcastsByCategory( PodcastUtil.WeekendVideos_CategoryId, false, 3 );
            if ( rootCategory != null )
            {
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
            }

            // after all the searching above, if we have a latest message, yay.
            return latestMessage;
        }
    }
}
