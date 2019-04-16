using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using church.ccv.PersonalizationEngine.Model;
using church.ccv.PersonalizationEngine.Data;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest.Filters;
using Rock.Web.Cache;
using Newtonsoft.Json.Linq;
using System.Web.Http;
using church.ccv.CCVRest.MobileApp.Model;
using church.ccv.Podcast;

namespace church.ccv.CCVRest.MobileApp
{
    public partial class NewMobileAppController : Rock.Rest.ApiControllerBase
    {
        [Serializable]
        public enum VersionResponse
        {
            NotSet = -1,

            Success,

            Failed
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Version" )]
        [Authenticate, Secured]
        public HttpResponseMessage Version()
        {
            // the attribute Id for the Mobile App's version
            const int MobileAppVersionAttributeId = 29469;

            // find the mobile app version Global Attribute and return it
            RockContext rockContext = new RockContext();
            var mobileAppAttribute = new AttributeValueService( rockContext ).Queryable().AsNoTracking().Where( av => av.AttributeId == MobileAppVersionAttributeId ).SingleOrDefault();
            if ( mobileAppAttribute != null )
            {
                int mobileAppVersion = 0;
                if ( int.TryParse( mobileAppAttribute.Value, out mobileAppVersion ) )
                {
                    return Common.Util.GenerateResponse( true, VersionResponse.Success.ToString(), mobileAppVersion.ToString() );
                }
            }

            return Common.Util.GenerateResponse( false, VersionResponse.Failed.ToString(), null );
        }

        [Serializable]
        public enum PersonalizedContentResponse
        {
            NotSet = -1,

            Success,

            NoCampaignsFound,
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/PersonalizedContent" )]
        [Authenticate, Secured]
        public HttpResponseMessage GetPersonalizedContent( int numCampaigns = 1, int? primaryAliasId = null )
        {
            const string PersonalizationEngine_MobileAppNewsFeed_Key = "MobileAppNewsFeed";

            // assume we'll need default campaigns
            bool useDefaultCampaigns = true;

            List<Campaign> campaignResults = new List<Campaign>();

            if ( primaryAliasId.HasValue )
            {
                // get the personId
                PersonAliasService paService = new PersonAliasService( new RockContext() );
                PersonAlias personAlias = paService.Get( primaryAliasId.Value );

                if ( personAlias != null )
                {
                    // try getting campaigns for his person
                    campaignResults = PersonalizationEngineUtil.GetRelevantCampaign( PersonalizationEngine_MobileAppNewsFeed_Key, personAlias.PersonId, numCampaigns );

                    // and if we found at least 1, then we won't need default
                    if ( campaignResults.Count > 0 )
                    {
                        useDefaultCampaigns = false;
                    }
                }
            }

            // if we got down here, either they want default campaigns, or there weren't any relevant campaigns for the user
            if ( useDefaultCampaigns )
            {
                campaignResults = PersonalizationEngineUtil.GetDefaultCampaign( PersonalizationEngine_MobileAppNewsFeed_Key, numCampaigns );
            }

            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

            // now parse the campaigns and extract the relevant stuff out of the MobileAppNewsFeed section
            List<Model.PersonalizedItem> itemsList = new List<MobileApp.Model.PersonalizedItem>();
            foreach ( Campaign campaign in campaignResults )
            {
                JObject contentBlob = JObject.Parse( campaign["ContentJson"].ToString() );
                JObject mobileAppNewsFeedBlob = JObject.Parse( contentBlob[PersonalizationEngine_MobileAppNewsFeed_Key].ToString() );

                // try getting values for each piece of the campaign
                Model.PersonalizedItem psItem = new MobileApp.Model.PersonalizedItem
                {
                    Title = mobileAppNewsFeedBlob["title"].ToString(),
                    Description = mobileAppNewsFeedBlob["body"].ToString(),
                    DetailsURL = mobileAppNewsFeedBlob["link"].ToString(),
                    ImageURL = mobileAppNewsFeedBlob["img"].ToString(),
                    SkipDetailsPage = mobileAppNewsFeedBlob["skip-details-page"].ToString().AsBoolean(),

                    // For future compatibility
                    LaunchExternalBrowser = false,
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

            // if there's at least 1 campaign to return, respond 
            if ( itemsList.Count > 0 )
            {
                return Common.Util.GenerateResponse( true, PersonalizedContentResponse.Success.ToString(), itemsList );
            }
            else
            {
                return Common.Util.GenerateResponse( false, PersonalizedContentResponse.NoCampaignsFound.ToString(), null );
            }
        }

        [Serializable]
        public enum PromotionsResponse
        {
            NotSet = -1,

            Success
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Promotions" )]
        [Authenticate, Secured]
        public HttpResponseMessage GetPromotions( bool includeUnpublished = false )
        {
            const int MobileApp_ContentChannelId = 5;

            RockContext rockContext = new RockContext();
            var ccItemQuery = new ContentChannelItemService( rockContext ).Queryable().AsNoTracking();

            var ccItems = ccItemQuery.Where( cci =>
                cci.ContentChannelId == MobileApp_ContentChannelId && //Get all mobile app ads
              ( cci.Status == ContentChannelItemStatus.Approved || ( cci.Status == ContentChannelItemStatus.PendingApproval && includeUnpublished == true ) ) && //That are approved (or Pending AND includeUnpublished is on)
              ( cci.StartDateTime < DateTime.Now || includeUnpublished == true ) && //That have started running (or includeUnpublished is on)
              ( cci.ExpireDateTime == null || cci.ExpireDateTime >= DateTime.Now || includeUnpublished == true ) ) //That have not expired, or have no expiration date (or includeUnpublished is on)

            .ToList();

            List<Model.Promotion> promotions = new List<MobileApp.Model.Promotion>();

            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

            // load all the extended attributes for the item
            foreach ( ContentChannelItem item in ccItems )
            {
                item.LoadAttributes();

                // now package up just what the mobile app needs to reduce data sent
                Model.Promotion promotion = new MobileApp.Model.Promotion
                {
                    SortPriority = item.Priority,

                    ImageURL = publicAppRoot + "GetImage.ashx?Guid=" + item.GetAttributeValue( "FeatureImage" ),

                    Title = item.Title,
                    Description = item.Content,

                    SkipDetailsPage = item.GetAttributeValue( "MobileAppSkipDetailsPage" ).AsBoolean(),

                    DetailsURL = item.GetAttributeValue( "DetailsURL" ),
                    LaunchExternalBrowser = item.GetAttributeValue( "DetailsURLLaunchesBrowser" ).AsBoolean(),
                    IncludeAccessToken = item.GetAttributeValue( "IncludeImpersonationToken" ).AsBoolean()
                };

                promotions.Add( promotion );
            }

            //sort them
            promotions.Sort( delegate ( Model.Promotion a, Model.Promotion b )
            {
                return a.SortPriority < b.SortPriority ? -1 : 1;
            } );

            // return it!
            return Common.Util.GenerateResponse( true, PromotionsResponse.Success.ToString(), promotions );
        }

        [Serializable]
        public enum CampusResponse
        {
            NotSet = -1,

            Success,

            CampusNotFound
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Campus" )]
        [Authenticate, Secured]
        public HttpResponseMessage GetCampus( int? campusId = null )
        {
            // get one or all campuses, depending on what they ask for
            List<CampusCache> campusCacheList = new List<CampusCache>();

            if ( campusId.HasValue )
            {
                CampusCache campusCache = CampusCache.Read( campusId.Value );
                if ( campusCache != null )
                {
                    campusCacheList.Add( campusCache );
                }
            }
            else
            {
                // get all active campuses (false means don't include inactive ones)
                campusCacheList = CampusCache.All( false );
            }

            // if we couldn't load any campuses, they asked for one that doesn't exist
            if ( campusCacheList.Count == 0 )
            {
                return Common.Util.GenerateResponse( false, CampusResponse.CampusNotFound.ToString(), null );
            }

            // now begin the wild joining to get all necessary data
            RockContext rockContext = new RockContext();
            PersonAliasService paService = new PersonAliasService( rockContext );
            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

            List<Model.CampusModel> campusModelList = new List<MobileApp.Model.CampusModel>();

            foreach ( CampusCache campusCache in campusCacheList )
            {
                // first copy over simple native types from the model
                Model.CampusModel campusModel = new Model.CampusModel
                {
                    Id = campusCache.Id,
                    Name = campusCache.Name,

                    PhoneNumber = campusCache.PhoneNumber,

                    Latitude = campusCache.Location.Latitude.Value,
                    Longitude = campusCache.Location.Longitude.Value,

                    Street = campusCache.Location.Street1,
                    City = campusCache.Location.City,
                    State = campusCache.Location.State,
                    Zip = campusCache.Location.PostalCode,

                    DistanceFromSource = 0 //TODO: Do we support api driven distance calcs?
                };

                // Grab Campus Pastor Info
                if ( campusCache.LeaderPersonAliasId.HasValue )
                {
                    PersonAlias campusPastor = paService.Get( campusCache.LeaderPersonAliasId.Value );

                    campusModel.CampusPastorName = campusPastor.Person.NickName + " " + campusPastor.Person.LastName;
                    campusModel.CampusPastorEmail = campusPastor.Person.Email;

                    if ( campusPastor.Person.PhotoId.HasValue )
                    {
                        campusModel.CampusPastorImageURL = publicAppRoot + "GetImage.ashx?Id=" + campusPastor.Person.PhotoId.Value;
                    }
                    else
                    {
                        campusModel.CampusPastorImageURL = string.Empty;
                    }
                }

                // build the wistia video URL
                var wistiaIdAV = campusCache.AttributeValues["CampusTourWistiaId"];
                if ( wistiaIdAV != null )
                {
                    //todo: figure out how we'll link to this video
                    campusModel.VideoURL = wistiaIdAV.ToString();
                }

                // include the campus image (in case the wistia video isn't available)
                var photoAV = campusCache.AttributeValues["MarketingSiteCoverPhoto"];
                if ( photoAV != null )
                {
                    campusModel.ImageURL = publicAppRoot + "GetImage.ashx?Guid=" + photoAV.Value;
                }

                // Service Times
                campusModel.ServiceTimes = new List<MobileApp.Model.ServiceTimeModel>();
                foreach ( var campusCacheServiceTime in campusCache.ServiceTimes )
                {
                    Model.ServiceTimeModel serviceTime = new MobileApp.Model.ServiceTimeModel();

                    // check for special symbols
                    if ( campusCacheServiceTime.Time.Contains( '%' ) )
                    {
                        serviceTime.SpecialNeeds = true;
                    }

                    if ( campusCacheServiceTime.Time.Contains( '*' ) )
                    {
                        serviceTime.HearingImpaired = true;
                    }

                    // now cut out those symbols and trailing whitespace
                    serviceTime.Time = campusCacheServiceTime.Time.Trim( new char[] { '%', '*' } ).Trim();

                    serviceTime.Day = campusCacheServiceTime.Day;

                    campusModel.ServiceTimes.Add( serviceTime );
                }

                // Seventh Grade
                var serviceLocationAV = campusCache.AttributeValues["7thGradeServiceLocation"];
                if ( serviceLocationAV != null )
                {
                    campusModel.SeventhGrade_ServiceLocation = serviceLocationAV.ToString();
                }

                var serviceTimeAV = campusCache.AttributeValues["7thGradeServiceTime"];
                if ( serviceTimeAV != null )
                {
                    campusModel.SeventhGrade_ServiceTime = serviceTimeAV.ToString();
                }

                // Eighth Grade
                serviceLocationAV = campusCache.AttributeValues["8thGradeServiceLocation"];
                if ( serviceLocationAV != null )
                {
                    campusModel.EighthGrade_ServiceLocation = serviceLocationAV.ToString();
                }

                serviceTimeAV = campusCache.AttributeValues["8thGradeServiceTime"];
                if ( serviceTimeAV != null )
                {
                    campusModel.EighthGrade_ServiceTime = serviceTimeAV.ToString();
                }

                // High School
                serviceLocationAV = campusCache.AttributeValues["HighSchoolLocations"];
                if ( serviceLocationAV != null )
                {
                    campusModel.HighSchool_ServiceLocation = serviceLocationAV.ToString();
                }

                serviceTimeAV = campusCache.AttributeValues["HighSchoolTime"];
                if ( serviceTimeAV != null )
                {
                    campusModel.HighSchool_ServiceTime = serviceTimeAV.ToString();
                }

                // Kids
                campusModel.Kids_ServiceTime = "Available during all services";
                campusModel.Kids_ServiceLocation = string.Empty;

                // Misc Data
                var campusCacheAV = campusCache.AttributeValues["History"];
                if ( campusCacheAV != null )
                {
                    campusModel.Info_About = campusCacheAV.ToString();
                }

                campusCacheAV = campusCache.AttributeValues["ParkingDirections"];
                if ( campusCacheAV != null )
                {
                    // parking directions are weird--we'll split it up to make it easier for the mobile app
                    string[] stepsArray = campusCacheAV.ToString().Split( '\n' );

                    campusModel.Info_ParkingDirectionSteps = new List<string>();
                    foreach ( string step in stepsArray )
                    {
                        // now for each step, remove any leading bullets or whitespace
                        string cleanedString = step.TrimStart( new char[] { ' ', '*' } );
                        campusModel.Info_ParkingDirectionSteps.Add( cleanedString );
                    }

                }

                // the map URL is defined by the following format
                campusModel.Info_MapImageURL = publicAppRoot + "/Themes/church_ccv_External_v8/assets/images/home/locations/campus-landing/campus-maps/map-" + campusCache.ShortCode.ToLower() + ".jpg";

                // and these values are hardcoded
                campusModel.Info_FirstTimeArrival = "On your first visit, look for the designated New to CCV Guest Tables," +
                                                    " where one of our team members will provide you with a Welcome Packet and answer any questions you may have." +
                                                    " If you are checking in children, they will cover the first - time visit check-in process and lead you to your child's classroom." +
                                                    " You'll want to arrive about 15 - 20 minutes early.";

                campusModel.Info_CheckingInKids = "Parents must check in their kids before service. Once your child is registered for the first time," +
                                                  " simply enter your phone number into one of the self-service or assisted kiosks to receive your child's name tag." +
                                                  " You will receive a matching pick-up receipt which will be required to pick your child up after service." +
                                                  " Jr High and High School Students are able to check themselves into class using the self-service kiosks.";

                campusModelList.Add( campusModel );
            }

            return Common.Util.GenerateResponse( true, CampusResponse.Success.ToString(), campusModelList );
        }

        [Serializable]
        public enum IsOnCampusResponse
        {
            NotSet = -1,

            Success,

            NotOnCampus,

            InvalidModel,

            InvalidLatitude,

            InvalidLongitude
        }

        [System.Web.Http.HttpPost]
        [System.Web.Http.Route( "api/NewMobileApp/IsOnCampus" )]
        [Authenticate, Secured]
        public HttpResponseMessage IsOnCampus( [FromBody] IsOnCampusModel isOnCampusModel )
        {
            // validate the model
            if ( isOnCampusModel == null )
            {
                return Common.Util.GenerateResponse( false, IsOnCampusResponse.InvalidModel.ToString(), null );
            }
            // validate latitude is -90 to 90
            else if ( Math.Abs( isOnCampusModel.Latitude ) > 90 )
            {
                return Common.Util.GenerateResponse( false, IsOnCampusResponse.InvalidLatitude.ToString(), null );
            }
            // validate longitude is -180 to 180
            else if ( Math.Abs( isOnCampusModel.Longitude ) > 180 )
            {
                return Common.Util.GenerateResponse( false, IsOnCampusResponse.InvalidLongitude.ToString(), null );
            }

            RockContext rockContext = new RockContext();

            // see if the location is within maxDistanceMeters meters of the campus
            double maxDistanceMeters = 750;
            int? campusForLocation = MobileAppService.GetNearestCampus( isOnCampusModel.Longitude, isOnCampusModel.Latitude, maxDistanceMeters );
            if ( campusForLocation.HasValue )
            {
                // Send back a response that says yes, and includes the campusId for the campus they're on.
                return Common.Util.GenerateResponse( true, IsOnCampusResponse.Success.ToString(), campusForLocation.Value );
            }
            else
            {
                // send back that they aren't on campus
                return Common.Util.GenerateResponse( true, IsOnCampusResponse.NotOnCampus.ToString(), null );
            }
        }

        [Serializable]
        public enum KidsContentResponse
        {
            NotSet = -1,

            Success,

            PersonNotFound
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/KidsContent" )]
        [Authenticate, Secured]
        public HttpResponseMessage KidsContent( int primaryAliasId )
        {
            // find the person thru their alias id
            PersonAliasService paService = new PersonAliasService( new RockContext() );
            PersonAlias personAlias = paService.Get( primaryAliasId );

            if ( personAlias != null )
            {
                // we found them - now get the correct content
                KidsContentModel contentModel = MobileAppService.BuildKidsContent( personAlias.Person );
                return Common.Util.GenerateResponse( true, KidsContentResponse.Success.ToString(), contentModel );
            }
            else
            {
                return Common.Util.GenerateResponse( false, KidsContentResponse.PersonNotFound.ToString(), null );
            }
        }

    }
}
