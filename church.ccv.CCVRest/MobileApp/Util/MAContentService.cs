using System;
using System.Collections.Generic;
using System.Data.Entity.Spatial;
using System.Linq;
using church.ccv.CCVRest.MobileApp.Model;
using church.ccv.PersonalizationEngine.Data;
using church.ccv.PersonalizationEngine.Model;
using Newtonsoft.Json.Linq;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace church.ccv.CCVRest.MobileApp
{
    public class MAContentService
    {
        public static List<PersonalizedItem> GetPreGatePersonalizedContent( int numCampaigns, bool includeAllOverride = false )
        {
            const int ContentChannelId_PreGate = 318;

            RockContext rockContext = new RockContext();
            ContentChannelService contentChannelService = new ContentChannelService( rockContext );

            ContentChannel pregateContent = contentChannelService.Get( ContentChannelId_PreGate );

            List<Model.PersonalizedItem> itemsList = new List<MobileApp.Model.PersonalizedItem>();
            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

            foreach ( var item in pregateContent.Items )
            {
                item.LoadAttributes();

                bool isActive = item.AttributeValues["Active"].ToString().AsBoolean();
                if ( isActive == true )
                {
                    Model.PersonalizedItem psItem = new MobileApp.Model.PersonalizedItem();

                    psItem.Title = item.AttributeValues["PreGate_Title"].ToString();
                    psItem.SubTitle = item.AttributeValues["SubTitle"].ToString();
                    psItem.DetailsBody = item.AttributeValues["DetailsBody"].ToString();
                    psItem.DetailsURL = item.AttributeValues["Link"].ToString();
                    psItem.SortPriority = item.AttributeValues["SortPriority"].ToString().AsInteger();
                    psItem.SkipDetailsPage = item.AttributeValues["SkipDetailsPage"].ToString().AsBoolean();
                    psItem.LaunchesExternalBrowser = item.AttributeValues["LaunchesExternalBrowser"].ToString().AsBoolean();

                    string imageGuid = item.AttributeValues["Image"].Value.ToString();
                    if ( string.IsNullOrWhiteSpace( imageGuid ) == false )
                    {
                        psItem.ImageURL = publicAppRoot + "GetImage.ashx?Guid=" + imageGuid + "&width=1200";
                    }
                    else
                    {
                        psItem.ImageURL = string.Empty;
                    }

                    // we always want access tokens for personalization engine stuff
                    psItem.IncludeAccessToken = true;

                    itemsList.Add( psItem );
                }
            }

            //sort them
            itemsList = itemsList.OrderBy( i => i.SortPriority ).ToList();

            // now take only the number they asked for
            // this is not the most efficient--loading and building everything just to throw things out, but i'm tired
            // and we need to ship this.
            if ( includeAllOverride == false )
            {
                itemsList = itemsList.Take( numCampaigns ).ToList();
            }

            return itemsList;
        }

        public static List<PersonalizedItem> GetPersonalizedItems( int numCampaigns, int personId, bool includeAllOverride = false )
        {
            const string PersonalizationEngine_MobileAppJustForYou_Key = "MobileApp_JustForYou";
            List<Campaign> campaignResults = new List<Campaign>();

            // if includeAllOverride is true, we'll treat this as a debug mode, and include every campaign that's active.
            if ( includeAllOverride )
            {
                campaignResults = PersonalizationEngineUtil.GetCampaigns( PersonalizationEngine_MobileAppJustForYou_Key, DateTime.Now, DateTime.Now );
            }
            else
            {
                // try getting campaigns for this person
                campaignResults = PersonalizationEngineUtil.GetRelevantCampaign( PersonalizationEngine_MobileAppJustForYou_Key, personId, numCampaigns );
            }

            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

            // now parse the campaigns and extract the relevant stuff out of the MobileAppNewsFeed section
            List<Model.PersonalizedItem> itemsList = new List<MobileApp.Model.PersonalizedItem>();
            foreach ( Campaign campaign in campaignResults )
            {
                JObject contentBlob = JObject.Parse( campaign["ContentJson"].ToString() );
                JObject mobileAppBlob = JObject.Parse( contentBlob[PersonalizationEngine_MobileAppJustForYou_Key].ToString() );

                // try getting values for each piece of the campaign
                Model.PersonalizedItem psItem = new MobileApp.Model.PersonalizedItem
                {
                    Title = mobileAppBlob["title"].ToString(),
                    SubTitle = mobileAppBlob["subtitle"].ToString(),
                    DetailsBody = mobileAppBlob["detailsbody"].ToString(),
                    DetailsURL = mobileAppBlob["link"].ToString(),
                    ImageURL = mobileAppBlob["img"].ToString(),
                    SkipDetailsPage = mobileAppBlob["skip-details-page"].ToString().AsBoolean(),
                    LaunchesExternalBrowser = mobileAppBlob["launches-external-browser"].ToString().AsBoolean(),

                    // we always want access tokens for personalization engine stuff
                    IncludeAccessToken = true
                };

                // if either the details or image URL are relative, make them absolute
                if ( psItem.DetailsURL.StartsWith( "/" ) )
                {
                    psItem.DetailsURL = publicAppRoot + psItem.DetailsURL;
                }

                if ( psItem.ImageURL.StartsWith( "/" ) )
                {
                    psItem.ImageURL = publicAppRoot + psItem.ImageURL;
                }

                itemsList.Add( psItem );
            }

            return itemsList;
        }

        public static List<Promotion> GetPromotions( bool includeUnpublished = false )
        {
            const int ContentChannelId_Promotions = 319;

            RockContext rockContext = new RockContext();
            ContentChannelService contentChannelService = new ContentChannelService( rockContext );

            ContentChannel promotionContent = contentChannelService.Get( ContentChannelId_Promotions );

            List<Model.Promotion> itemsList = new List<MobileApp.Model.Promotion>();
            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

            foreach ( var item in promotionContent.Items )
            {
                item.LoadAttributes();

                // if it's active
                bool isActive = item.AttributeValues["Active"].ToString().AsBoolean();
                if ( isActive == true )
                {
                    // if it's not expired
                    DateTime? startDate = item.AttributeValues["Start"].ToString().AsDateTime();
                    DateTime? endDate = item.AttributeValues["End"].ToString().AsDateTime();
                    if ( ( (startDate == null || startDate < DateTime.Now) && ( endDate == null || endDate >= DateTime.Now) ) 
                        || includeUnpublished == true )
                    {
                        Model.Promotion promoItem = new MobileApp.Model.Promotion();

                        promoItem.Title = item.AttributeValues["NE_Title"].ToString();
                        promoItem.Description = item.AttributeValues["DetailsBody"].ToString();
                        promoItem.DetailsURL = item.AttributeValues["Link"].ToString();
                        promoItem.SortPriority = item.AttributeValues["SortPriority"].ToString().AsInteger();
                        promoItem.SkipDetailsPage = item.AttributeValues["SkipDetailsPage"].ToString().AsBoolean();
                        promoItem.LaunchesExternalBrowser = item.AttributeValues["LaunchesExternalBrowser"].ToString().AsBoolean();

                        string imageGuid = item.AttributeValues["Image"].Value.ToString();
                        if ( string.IsNullOrWhiteSpace( imageGuid ) == false )
                        {
                            promoItem.ImageURL = publicAppRoot + "GetImage.ashx?Guid=" + imageGuid + "&width=1200";
                            promoItem.ThumbnailImageURL = publicAppRoot + "GetImage.ashx?Guid=" + imageGuid + "&width=825";
                        }
                        else
                        {
                            promoItem.ImageURL = string.Empty;
                        }

                        promoItem.IncludeAccessToken = item.AttributeValues["ForwardUserIdentity"].ToString().AsBoolean();

                        itemsList.Add( promoItem );
                    }
                }
            }

            //sort them
            itemsList = itemsList.OrderBy( i => i.SortPriority ).ToList();

            return itemsList;
        }

        public static int? GetNearestCampus( double longitude, double latitude, double maxDistanceMeters )
        {
            List<CampusCache> campusCacheList = CampusCache.All( false );

            // assume we're too far away from any campuses
            double closestDistance = maxDistanceMeters;
            int? closestCampusId = null;

            // take the provided long/lat and get a geoPoint out of it
            DbGeography geoPoint = DbGeography.FromText( string.Format( "POINT({0} {1})", longitude, latitude ) );

            // now go thru each campus
            foreach ( CampusCache campusCache in campusCacheList )
            {
                // if the campus has a geopoint defined
                if ( campusCache.Location.Longitude.HasValue && campusCache.Location.Latitude.HasValue )
                {
                    // put it in a geoPoint
                    DbGeography campusGeoPoint = DbGeography.FromText( string.Format( "POINT({0} {1})", campusCache.Location.Longitude, campusCache.Location.Latitude ) );

                    // take the distance between the the provided point and this campus
                    double? distanceFromCampus = campusGeoPoint.Distance( geoPoint );

                    // if a calcluation could be performed and it's closer than what we've already found, take it.
                    if ( distanceFromCampus.HasValue && distanceFromCampus < closestDistance )
                    {
                        closestDistance = distanceFromCampus.Value;
                        closestCampusId = campusCache.Id;
                    }
                }
            }

            // return whatever the closest campus was (or null if none were close enough)
            return closestCampusId;
        }

        public static KidsContentModel BuildKidsContent( Person person )
        {
            const string MAMyFamilyContentOverrideKey = "MobileAppKidContentOverride";
            const string MAMyFamilyContentLevelKey = "MobileAppKidContentLevel";

            // see if the person has an override that sets their 
            // content level (Common among people with Special Needs)
            person.LoadAttributes();
            string targetContent = person.AttributeValues[MAMyFamilyContentOverrideKey]?.ToString();

            // if blank, there's no override, so choose content based on their grade/age
            if( string.IsNullOrWhiteSpace( targetContent ) == true )
            {
                // this is technically cheating, but Rock abstracts grade and doesn't natively
                // know about the US standard. To simplify things, let's do the conversion here
                int realGrade = -1; //(assume infant / pre-k)
                if ( person.GradeOffset.HasValue )
                {
                    realGrade = 12 - person.GradeOffset.Value;
                }
                else
                {
                    // no grade, so try using their age
                    if ( person.Age.HasValue )
                    {
                        // Kids 14+ get High School
                        if ( person.Age >= 14 )
                        {
                            realGrade = 9;
                        }
                        // Kids 12+ get Junior High
                        else if ( person.Age >= 12 )
                        {
                            realGrade = 7;
                        }
                        // Kids 7+ get Later Kids
                        else if ( person.Age >= 7 )
                        {
                            realGrade = 2;
                        }
                        // Kids 3+ get early Kids
                        else if ( person.Age >= 3 )
                        {
                            realGrade = 0;
                        }
                        // Kids < 3 get Infants
                        else
                        {
                            realGrade = -1;
                        }
                    }
                }

                // Now that we've resolved a grade, pick the content
                if ( realGrade >= 9 )
                {
                    targetContent = "High School";
                }
                else if ( realGrade >= 7 )
                {
                    targetContent = "Junior High";
                }
                else if ( realGrade >= 2 )
                {
                    targetContent = "Later Kids";
                }
                else if ( realGrade >= 0 )
                {
                    targetContent = "Early Kids";
                }
                else
                {
                    targetContent = "Infants";
                }
            }

            // now that we know the range, build the content channel queries
            RockContext rockContext = new RockContext();
            ContentChannelService contentChannelService = new ContentChannelService( rockContext );

            // first, get AtCCV
            const int ContentChannelId_AtCCV = 286;
            ContentChannel atCCV = contentChannelService.Get( ContentChannelId_AtCCV );

            // sort by date
            var atCCVItems = atCCV.Items.OrderByDescending( i => i.CreatedDateTime );

            // now take the first one that matches our grade offset.

            // while iterating over these in memory could become slow as the list grows, the business
            // requirements of CCV mean it won't. Because there will always be a new entry each week for each grade level,
            // it won't ever realistically go over the first 4 items
            ContentChannelItem atCCVItem = null;
            foreach ( var item in atCCVItems )
            {
                // this is the slow part. If it ever does become an issue, replace it with an AV table join.
                item.LoadAttributes();
                if ( item.AttributeValues[MAMyFamilyContentLevelKey].ToString() == targetContent )
                {
                    atCCVItem = item;
                    break;
                }
            }


            // next, get Faith Building At Home
            const int ContentChannelId_FaithBuilding = 287;
            ContentChannel faithBuilding = contentChannelService.Get( ContentChannelId_FaithBuilding );

            // sort by date
            var faithBuildingItems = faithBuilding.Items.OrderByDescending( i => i.CreatedDateTime );

            // as above, we'll iterate over the whole list in memory, knowing we'll actually only load attributes for about 4 items.
            ContentChannelItem faithBuildingItem = null;
            foreach ( var item in faithBuildingItems )
            {
                item.LoadAttributes();
                if ( item.AttributeValues[MAMyFamilyContentLevelKey].ToString() == targetContent )
                {
                    faithBuildingItem = item;
                    break;
                }
            }


            // finally, get the resources available for the grade level
            const int ContentChannelId_Resources = 288;
            ContentChannel resourceChannel = contentChannelService.Get( ContentChannelId_Resources );

            List<ContentChannelItem> resourceList = new List<ContentChannelItem>();
            foreach ( var item in resourceChannel.Items )
            {
                item.LoadAttributes();
                if ( item.AttributeValues[MAMyFamilyContentLevelKey].ToString().Contains( targetContent ) )
                {
                    resourceList.Add( item );
                }
            }

            // sort the resource list by priority
            resourceList.Sort( delegate ( ContentChannelItem a, ContentChannelItem b )
            {
                if ( a.Priority < b.Priority )
                {
                    return -1;
                }
                else if ( a.Priority == b.Priority )
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            } );


            // prepare our model - we'll require both main category items 
            // and otherwise return failure (note that resources CAN be empty)
            if ( atCCVItem != null && faithBuildingItem != null )
            {
                string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

                KidsContentModel contentModel = new KidsContentModel();

                // At CCV
                contentModel.AtCCV_Title = atCCVItem.AttributeValues["AtCCVTitle"].ToString();
                contentModel.AtCCV_Content = atCCVItem.AttributeValues["AtCCVContent"].ToString();
                contentModel.AtCCV_DiscussionTopic_One = atCCVItem.AttributeValues["DiscussionTopic1"].ToString();
                contentModel.AtCCV_DiscussionTopic_Two = atCCVItem.AttributeValues["DiscussionTopic2"].ToString();

                string dateTime = atCCVItem.AttributeValues["WeekendDate"].ToString();
                if ( string.IsNullOrWhiteSpace( dateTime ) == false )
                {
                    contentModel.AtCCV_Date = DateTime.Parse( dateTime );
                }

                string seriesImageGuid = atCCVItem.AttributeValues["SeriesImage"].Value.ToString();
                if ( string.IsNullOrWhiteSpace( seriesImageGuid ) == false )
                {
                    contentModel.AtCCV_ImageURL = publicAppRoot + "GetImage.ashx?Guid=" + seriesImageGuid + "&width=1200";
                }
                else
                {
                    contentModel.AtCCV_ImageURL = string.Empty;
                }

                // Faith building
                contentModel.FaithBuilding_Title = faithBuildingItem.AttributeValues["FBTitle"].ToString();
                contentModel.FaithBuilding_Content = faithBuildingItem.AttributeValues["FBContent"].ToString();

                // resources CAN be empty, so just take whatever's available
                contentModel.Resources = new List<KidsResourceModel>();

                foreach ( var resourceItem in resourceList )
                {
                    KidsResourceModel resModel = new KidsResourceModel
                    {
                        Title = resourceItem.AttributeValues["ResourceTitle"].ToString(),
                        Subtitle = resourceItem.AttributeValues["Subtitle"].ToString(),
                        URL = resourceItem.AttributeValues["URL"].ToString()
                    };

                    // is there a 'launches external browser' flag?
                    if ( resourceItem.ContainsKey( "LaunchesExternalBrowser" ) == true )
                    {
                        resModel.LaunchesExternalBrowser = resourceItem.AttributeValues["LaunchesExternalBrowser"].Value.AsBoolean();
                    }

                    contentModel.Resources.Add( resModel );
                }

                return contentModel;
            }
            else
            {
                return null;
            }
        }

        public static List<LifeTrainingTopicModel> BuildLifeTrainingContent( )
        {
            RockContext rockContext = new RockContext();
            ContentChannelService contentChannelService = new ContentChannelService( rockContext );

            // first, get the Life Training Topics
            const int ContentChannelId_LifeTrainingTopics = 295;
            ContentChannel lifeTrainingTopics  = contentChannelService.Get( ContentChannelId_LifeTrainingTopics );

            // sort by priority
            var ltTopicItems = lifeTrainingTopics.Items;


            // next, get the Life Training Resources
            const int ContentChannelId_LifeTrainingResources = 296;
            ContentChannel ltResources = contentChannelService.Get( ContentChannelId_LifeTrainingResources );

            // sort by priority
            var ltResourceItems = ltResources.Items.OrderByDescending( i => i.Priority );

            // now build the list of models we'll send down
            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

            List<LifeTrainingTopicModel> ltTopicModels = new List<LifeTrainingTopicModel>();
            foreach ( var ltTopic in ltTopicItems )
            {
                ltTopic.LoadAttributes();

                LifeTrainingTopicModel ltTopicModel = new LifeTrainingTopicModel();
                ltTopicModel.Title = ltTopic.AttributeValues["LTTitle"].ToString();
                ltTopicModel.Content = ltTopic.AttributeValues["LTContent"].ToString();

                int.TryParse( ltTopic.AttributeValues["LTOrder"].Value, out ltTopicModel.SortPriority );

                // try getting the image
                string ltItemImageGuid = ltTopic.AttributeValues["Image"].Value.ToString();
                if ( string.IsNullOrWhiteSpace( ltItemImageGuid ) == false )
                {
                    ltTopicModel.ImageURL = publicAppRoot + "GetImage.ashx?Guid=" + ltItemImageGuid + "&width=1200";
                }
                else
                {
                    ltTopicModel.ImageURL = string.Empty;
                }

                // try getting the "Talk to Someone" URL
                string ltItemTalkURL = ltTopic.AttributeValues["TalktoSomeoneURL"].Value.ToString();
                if ( string.IsNullOrWhiteSpace( ltItemTalkURL ) == false )
                {
                    ltTopicModel.TalkToSomeoneURL = ltItemTalkURL;
                }
                else
                {
                    ltTopicModel.TalkToSomeoneURL = string.Empty;
                }

                // now add all associated resources
                ltTopicModel.Resources = new List<LifeTrainingResourceModel>();
                foreach ( var resource in ltResourceItems )
                {
                    resource.LoadAttributes();

                    // is this resource associated with this topic?
                    string associatedTopics = resource.AttributeValues["AssociatedLifeTrainingTopics"].Value.ToString();
                    if ( associatedTopics.Contains( ltTopicModel.Title ) )
                    {
                        // then add it
                        LifeTrainingResourceModel resourceModel = new LifeTrainingResourceModel();
                        resourceModel.Title = resource.AttributeValues["LTResourceTitle"].Value.ToString();
                        resourceModel.Content = resource.AttributeValues["LTResourceContent"].Value.ToString();
                        resourceModel.Author = resource.AttributeValues["Author"].Value.ToString();
                        resourceModel.URL = resource.AttributeValues["URL"].Value.ToString();

                        // add a hint for the mobile app so it knows whether to show a book detail page or not.
                        // if there's content, and an author, it's a book.
                        if ( string.IsNullOrWhiteSpace( resourceModel.Content ) == false &&
                            string.IsNullOrWhiteSpace( resourceModel.Author ) == false )
                        {
                            resourceModel.IsBook = true;
                        }

                        // is there an image?
                        string resourceImageGuid = resource.AttributeValues["Image"].Value.ToString();
                        if ( string.IsNullOrWhiteSpace( resourceImageGuid ) == false )
                        {
                            resourceModel.ImageURL = publicAppRoot + "GetImage.ashx?Guid=" + resourceImageGuid + "&width=400";
                        }
                        else
                        {
                            resourceModel.ImageURL = string.Empty;
                        }

                        // is there a 'launches external browser' flag?
                        if ( resource.ContainsKey( "LaunchesExternalBrowser" ) == true )
                        {
                            resourceModel.LaunchesExternalBrowser = resource.AttributeValues["LaunchesExternalBrowser"].Value.AsBoolean();
                        }

                        ltTopicModel.Resources.Add( resourceModel );
                    }
                }

                // sort resources with books on top
                ltTopicModel.Resources = ltTopicModel.Resources.OrderByDescending( a => a.IsBook ).ToList();

                ltTopicModels.Add( ltTopicModel );
            }

            // finally, sort the topics
            ltTopicModels = ltTopicModels.OrderByDescending( lt => lt.SortPriority ).ToList();

            return ltTopicModels;
        }
    }
}
