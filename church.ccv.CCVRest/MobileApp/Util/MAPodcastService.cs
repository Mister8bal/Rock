using System;
using System.Collections.Generic;
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
            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

            MASeriesModel maSeriesModel = new MASeriesModel();

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

            // set the images
            if ( string.IsNullOrWhiteSpace( series.Attributes["Image_16_9"] ) == false )
            {
                maSeriesModel.ImageURL = publicAppRoot + "GetImage.ashx?Guid=" + series.Attributes["Image_16_9"];
            }

            if ( string.IsNullOrWhiteSpace( series.Attributes["Image_1_1"] ) == false )
            {
                maSeriesModel.ThumbnailURL = publicAppRoot + "GetImage.ashx?Guid=" + series.Attributes["Image_1_1"];
            }

            // after we process messages, we'll use this value for deciding whether the Series is hidden.
            bool allMessagesHidden = true;

            // Now generate each message of the series
            maSeriesModel.Messages = new List<MobileAppMessageModel>();
            foreach ( PodcastUtil.PodcastMessage message in series.Messages )
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
                else
                {
                    // this message is _NOT_ hidden, therefore we can set allMessagesHidden to false
                    allMessagesHidden = false;
                }

                maMessageModel.Name = message.Name;
                maMessageModel.Speaker = message.Attributes["Speaker"];
                maMessageModel.Date = message.Date.Value.ToShortDateString();
                maMessageModel.ImageURL = maSeriesModel.ImageURL;
                maMessageModel.ThumbnailURL = maSeriesModel.ThumbnailURL;

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

                string shareUrlValue = message.Attributes["ShareUrl"];
                if ( string.IsNullOrWhiteSpace( shareUrlValue ) == false )
                {
                    maMessageModel.ShareURL = shareUrlValue;
                }

                string discussionGuideUrlValue = null;
                message.Attributes.TryGetValue( "DiscussionGuideUrl", out discussionGuideUrlValue );
                if ( string.IsNullOrWhiteSpace( discussionGuideUrlValue ) == false )
                {
                    maMessageModel.DiscussionGuideURL = discussionGuideUrlValue;
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

        public static List<ToolboxResourceModel> PodcastSeriesToToolboxResources( PodcastUtil.PodcastSeries series, ref int resourcesRemaining )
        {
            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

            // for toolbox resources, we don't store the series hierarhcy--each message is its own item,
            // so a single series could spawn multiple resource models
            List<ToolboxResourceModel> toolboxResources = new List<ToolboxResourceModel>();

            // use an i iterator so we can get the "WeekNumber" more easily
            int i = 0;
            for( i = 0; i < series.Messages.Count; i++ )
            //foreach ( PodcastUtil.PodcastMessage message in series.Messages )
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

                if ( messageActive && message.Date <= RockDateTime.Now  )
                {
                    resource.SeriesName = series.Name;
                    resource.MessageName = message.Name;

                    if ( string.IsNullOrWhiteSpace( series.Attributes["Image_16_9"] ) == false )
                    {
                        resource.SeriesImageURL = publicAppRoot + "GetImage.ashx?Guid=" + series.Attributes["Image_16_9"];
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
                        resource.WistiaId = wistiaId;
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
    }
}
