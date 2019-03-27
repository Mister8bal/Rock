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
        public HttpResponseMessage Version( )
        {
            // the attribute Id for the Mobile App's version
            const int MobileAppVersionAttributeId = 29469;
            
            // find the mobile app version Global Attribute and return it
            RockContext rockContext = new RockContext();
            var mobileAppAttribute = new AttributeValueService( rockContext ).Queryable().AsNoTracking( ).Where( av => av.AttributeId == MobileAppVersionAttributeId ).SingleOrDefault();
            if ( mobileAppAttribute != null )
            {
                int mobileAppVersion = 0;
                if ( int.TryParse( mobileAppAttribute.Value, out mobileAppVersion ) )
                {
                    return Common.Util.GenerateResponse( true, VersionResponse.Success.ToString(), mobileAppVersion.ToString() );
                }
            }

            return Common.Util.GenerateResponse( false, VersionResponse.Failed.ToString( ), null );
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
        public HttpResponseMessage GetPersonalizedContent(int numCampaigns = 1, int? primaryAliasId = null )
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
                    Title = mobileAppNewsFeedBlob["title"]?.ToString(),
                    Description = mobileAppNewsFeedBlob["body"]?.ToString(),
                    DetailsURL = mobileAppNewsFeedBlob["link"]?.ToString(),
                    ImageURL = mobileAppNewsFeedBlob["img"]?.ToString()
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
            if( itemsList.Count > 0 )
            {
                return Common.Util.GenerateResponse( true, PersonalizedContentResponse.Success.ToString( ), itemsList );
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
            var ccItemQuery = new ContentChannelItemService( rockContext ).Queryable( ).AsNoTracking( );

            var ccItems = ccItemQuery.Where( cci => 
                cci.ContentChannelId == MobileApp_ContentChannelId && //Get all mobile app ads
              ( cci.Status == ContentChannelItemStatus.Approved || (cci.Status == ContentChannelItemStatus.PendingApproval && includeUnpublished == true) ) && //That are approved (or Pending AND includeUnpublished is on)
              ( cci.StartDateTime < DateTime.Now || includeUnpublished == true ) && //That have started running (or includeUnpublished is on)
              ( cci.ExpireDateTime == null || cci.ExpireDateTime >= DateTime.Now || includeUnpublished == true ) ) //That have not expired, or have no expiration date (or includeUnpublished is on)
                                           
            .ToList();

            List<Model.Promotion> promotions = new List<MobileApp.Model.Promotion>( );

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

                    DetailsURL = item.GetAttributeValue( "DetailsURL" ),
                    DetailsURLLaunchesBrowser = item.GetAttributeValue( "DetailsURLLaunchesBrowser" ).AsBoolean( ),
                    IncludeImpersonationToken = item.GetAttributeValue( "IncludeImpersonationToken" ).AsBoolean( ),

                    SkipDetailsPage = item.GetAttributeValue( "MobileAppSkipDetailsPage" ).AsBoolean( ),

                    StartDateTime = item.StartDateTime,
                    EndDateTime = item.ExpireDateTime,

                    PublishedStatus = (int)item.Status
                };

                promotions.Add( promotion );
            }

            // return it!
            return Common.Util.GenerateResponse( true, PromotionsResponse.Success.ToString( ), promotions );
        }
    }
}
