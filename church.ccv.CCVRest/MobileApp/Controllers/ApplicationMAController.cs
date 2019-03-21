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

namespace church.ccv.CCVRest.MobileApp
{
    public partial class NewMobileAppController : Rock.Rest.ApiControllerBase
    {
        [Serializable]
        public enum VersionResponse
        {
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

            // if there's at least 1 campaign to return, respond 
            if( campaignResults.Count > 0 )
            {
                return Common.Util.GenerateResponse( true, PersonalizedContentResponse.Success.ToString( ), campaignResults );
            }
            else
            {
                return Common.Util.GenerateResponse( false, PersonalizedContentResponse.NoCampaignsFound.ToString(), null );
            }
        }
    }
}
