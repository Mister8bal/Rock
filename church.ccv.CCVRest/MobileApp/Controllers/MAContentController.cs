using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using church.ccv.CCVRest.Common.Model;
using church.ccv.CCVRest.MobileApp.Model;
using church.ccv.PersonalizationEngine.Data;
using church.ccv.PersonalizationEngine.Model;
using Newtonsoft.Json.Linq;
using Rock;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Rest.Filters;
using Rock.Web.Cache;

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
        public enum CCVLiveCountdownResponse
        {
            NotSet = -1,

            Success,

            BadResponse
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/CCVLiveCountdown" )]
        [Authenticate, Secured]
        public async Task<HttpResponseMessage> CCVLiveCountdown()
        {
            // make a request to the CCV Live API and get its status, then forward it to the client
            const string CCVLiveAPI = "https://ccvlive.churchonline.org/api/v1/events/current";

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync( CCVLiveAPI );
            if ( response.StatusCode != System.Net.HttpStatusCode.OK )
            {
                return Common.Util.GenerateResponse( false, CCVLiveCountdownResponse.BadResponse.ToString(), null );
            }

            // we expect the a json formatted response with an ""response->item" in it. If any parsing fails,
            // respond with BadResponse
            try
            {
                var countdownResponse = await response.Content.ReadAsAsync<JObject>();

                var responseJToken = countdownResponse["response"];
                JToken itemToken = responseJToken["item"];
                CCVLiveCountdownModel liveResponse = itemToken.ToObject<CCVLiveCountdownModel>();

                // now add the data for the latest message, since the countdown banner wants that as well.
                liveResponse.LatestMessage = MAPodcastService.GetLatestMessage();

                return Common.Util.GenerateResponse( true, CCVLiveCountdownResponse.Success.ToString(), liveResponse );
            }
            catch
            {
                return Common.Util.GenerateResponse( false, CCVLiveCountdownResponse.BadResponse.ToString(), null );
            }
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
        public HttpResponseMessage GetPersonalizedContent( int numCampaigns = 1, int? primaryAliasId = null, bool includeAllOverride = false )
        {
            // The PersonalizationEngine only works when given a specific person. So, begin by tring to get their Id
            int personId = 0;
            if ( primaryAliasId.HasValue && primaryAliasId != 0 )
            {
                // get the personId
                PersonAliasService paService = new PersonAliasService( new RockContext() );
                PersonAlias personAlias = paService.Get( primaryAliasId.Value );

                if ( personAlias != null )
                {
                    personId = personAlias.PersonId;
                }
            }

            List<PersonalizedItem> itemsList = null;

            // we have a person, so get personalized items
            if ( personId > 0 )
            {
                itemsList = MAContentService.GetPersonalizedItems( numCampaigns, personId, includeAllOverride );
            }
            // there's no person, so get preGate content
            else
            {
                itemsList = MAContentService.GetPreGatePersonalizedContent( numCampaigns, includeAllOverride );
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
            List<Model.Promotion> promotions = MAContentService.GetPromotions( includeUnpublished );

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
        public async Task<HttpResponseMessage> GetCampus( int? campusId = null )
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
                    campusPastor.Person.LoadAttributes();

                    // try to get their public name
                    AttributeValueCache publicNameAVCache = null;
                    campusPastor.Person.AttributeValues.TryGetValue( "PublicName", out publicNameAVCache );
                    if ( publicNameAVCache != null )
                    {
                        campusModel.CampusPastorName = publicNameAVCache.Value;
                    }
                    else
                    {
                        campusModel.CampusPastorName = string.Empty;
                    }

                    // try to get their public email
                    AttributeValueCache publicEmailAVCache = null;
                    campusPastor.Person.AttributeValues.TryGetValue( "PublicEmail", out publicEmailAVCache );
                    if ( publicEmailAVCache != null )
                    {
                        campusModel.CampusPastorEmail = publicEmailAVCache.Value;
                    }
                    else
                    {
                        campusModel.CampusPastorEmail = string.Empty;
                    }

                    // try to get their public image
                    AttributeValueCache publicPhotoAVCache = null;
                    campusPastor.Person.AttributeValues.TryGetValue( "PublicPhoto", out publicPhotoAVCache );
                    if ( publicPhotoAVCache != null )
                    {
                        campusModel.CampusPastorImageURL = publicAppRoot + "GetImage.ashx?Guid=" + publicPhotoAVCache.Value + "&width=180";
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
                    await Common.Util.GetWistiaMedia( wistiaIdAV.ToString(),
                            delegate ( HttpStatusCode statusCode, WistiaMedia media )
                            {
                                if ( statusCode == HttpStatusCode.OK )
                                {
                                    campusModel.VideoURL = Common.Util.GetWistiaAssetMpeg4URL( media, WistiaAsset.IPhoneVideoFile );
                                }
                            } );
                }

                // include the campus image (in case the wistia video isn't available)
                var photoAV = campusCache.AttributeValues["MarketingSiteCoverPhoto"];
                if ( photoAV != null )
                {
                    campusModel.ImageURL = publicAppRoot + "GetImage.ashx?Guid=" + photoAV.Value + "&width=1200";
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

                    if ( campusCacheServiceTime.Time.Contains( '#' ) )
                    {
                        serviceTime.SpanishTranslation = true;
                    }

                    // now cut out those symbols and trailing whitespace
                    serviceTime.Time = campusCacheServiceTime.Time.Trim( new char[] { '%', '*', '#' } ).Trim();

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
            int? campusForLocation = MAContentService.GetNearestCampus( isOnCampusModel.Longitude, isOnCampusModel.Latitude, maxDistanceMeters );
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

            PersonNotFound,

            ContentInvalid
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
                try
                {
                    KidsContentModel contentModel = MAContentService.BuildKidsContent( personAlias.Person );
                    return Common.Util.GenerateResponse( true, KidsContentResponse.Success.ToString(), contentModel );
                }
                catch
                {
                    // something went wrong with our content channels - at least let the caller know
                    return Common.Util.GenerateResponse( false, KidsContentResponse.ContentInvalid.ToString(), null );
                }
            }
            else
            {
                return Common.Util.GenerateResponse( false, KidsContentResponse.PersonNotFound.ToString(), null );
            }
        }

        [Serializable]
        public enum BaptismResponse
        {
            NotSet = -1,
            Success,
            CampusNotFound
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Baptism" )]
        [Authenticate, Secured]
        public HttpResponseMessage Baptism( int campusId, int maxCount = 4 )
        {
            // verify they are asking for a valid campus
            CampusCache campus = CampusCache.Read( campusId );
            if ( campus == null )
            {
                return Common.Util.GenerateResponse( false, BaptismResponse.CampusNotFound.ToString(), null );
            }

            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();
            const int BaptismEventId = 6;

            RockContext rockContext = new RockContext();

            // get event occurrences
            var eventItems = new EventItemOccurrenceService( rockContext ).Queryable().AsNoTracking()
                                                    // Take active Baptism events for this campus that have exactly ONE registration attached and ONE schedule attached
                                                    .Where( e => e.EventItem.IsActive && 
                                                                 e.EventItemId == BaptismEventId && 
                                                                 e.CampusId == campusId &&
                                                                 e.ScheduleId.HasValue &&
                                                                 e.Linkages.Count() == 1 &&
                                                                 e.Linkages.FirstOrDefault().RegistrationInstanceId.HasValue )

                                                    // And only with registrations that are open
                                                    .Where( e => e.Linkages.FirstOrDefault().RegistrationInstance.StartDateTime <= DateTime.Now &&
                                                                 e.Linkages.FirstOrDefault().RegistrationInstance.EndDateTime > DateTime.Now )
                                                    .ToList( );

            List<BaptismModel> baptisms = new List<BaptismModel>();
            foreach ( var eventItem in eventItems )
            {
                BaptismModel baptismModel = new BaptismModel();

                var dateList = RockFilters.DatesFromICal( (object) eventItem.Schedule.iCalendarContent, null, null );

                // in practice, a baptism event should have ONE date attached to it. Ignore anything else.
                if ( dateList.Count() == 1 )
                {
                    baptismModel.Date = dateList[0];

                    // Note that we're guaranteed Linkage and RegInstanceId are valid because we only take those from the query above
                    int regInstanceId = eventItem.Linkages.First().RegistrationInstanceId.Value;
                    baptismModel.RegistrationURL = publicAppRoot + "/get-involved/next-steps/baptism/registration?RegistrationInstanceId=" + regInstanceId + "&EventOccurrenceID=" + eventItem.Id;

                    baptisms.Add( baptismModel );
                }
            }


            // unfortunately we have to sort and take NOW, because
            // we need to sort by time, which isn't available in the linq query up top
            baptisms.Sort( delegate ( BaptismModel x, BaptismModel y )
            {
                if ( x.Date < y.Date )
                {
                    return -1;
                }
                else if ( x.Date == y.Date )
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            } );

            // and only take the number requested
            baptisms = baptisms.Take( maxCount ).ToList();

            return Common.Util.GenerateResponse( true, BaptismResponse.Success.ToString(), baptisms );
        }

        [Serializable]
        public enum LifeTrainingResponse
        {
            NotSet = -1,

            Success,

            ContentInvalid
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/LifeTraining" )]
        [Authenticate, Secured]
        public HttpResponseMessage LifeTraining( )
        {
            try
            {
                List<LifeTrainingTopicModel> topicModelList = MAContentService.BuildLifeTrainingContent();

                return Common.Util.GenerateResponse( true, LifeTrainingResponse.Success.ToString(), topicModelList );
            }
            catch
            {
                // something went wrong with our content channels - at least let the caller know
                return Common.Util.GenerateResponse( false, LifeTrainingResponse.ContentInvalid.ToString(), null );
            }
        }

        [Serializable]
        public enum SpanishTranslationAvailableResponse
        {
            NotSet = -1,

            Success,

            InvalidCampus
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/SpanishTranslationAvailable" )]
        [Authenticate, Secured]
        public HttpResponseMessage SpanishTranslationAvailable(int campusId)
        {
            try
            {
                //TODO: Look at the upcoming service time for the campus provided, and see if it offers spanish translation.
                // We should probably decide (here) on a window of time leading up to the service that we want it to say yes to.
                bool isAvailable = false;
                if ( campusId == 9 )
                {
                    isAvailable = true;
                }

                return Common.Util.GenerateResponse( true, SpanishTranslationAvailableResponse.Success.ToString(), isAvailable );
            }
            catch
            {
                // something went wrong with our content channels - at least let the caller know
                return Common.Util.GenerateResponse( false, SpanishTranslationAvailableResponse.InvalidCampus.ToString(), null );
            }
        }
    }
}
