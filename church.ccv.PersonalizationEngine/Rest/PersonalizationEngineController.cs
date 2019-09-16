using church.ccv.PersonalizationEngine.Data;
using church.ccv.PersonalizationEngine.Model;
using Newtonsoft.Json;
using Rock.Rest.Filters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using static church.ccv.PersonalizationEngine.Model.Campaign;

namespace church.ccv.PersonalizationEngine.Rest
{
    public class PersonalizationEngineController : Rock.Rest.ApiControllerBase
    {
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/PersonalizationEngine/Campaign" )]
        [System.Web.Http.Route( "api/PersonalizationEngine/RelevantCampaign" )]
        [Authenticate, Secured]
        public HttpResponseMessage GetCampaign( string campaignTypeList, int personId, int numCampaigns = 1 )
        {
            // now request the relevant campaign for the person
            List<Model.Campaign> campaignResults = PersonalizationEngineUtil.GetRelevantCampaign( campaignTypeList, personId, numCampaigns );

            // now, if we've got a result, return it. Otherwise throw an error
            HttpStatusCode statusCode = HttpStatusCode.OK;
            StringContent responseContent = null;

            if( campaignResults.Count > 0 )
            {
                responseContent = new StringContent( JsonConvert.SerializeObject( campaignResults ), Encoding.UTF8, "application/json" );
            }
            else
            {
                statusCode = HttpStatusCode.NotFound;
                responseContent = new StringContent( string.Format( "Could not find any campaigns for types of: {0}", campaignTypeList ) );
            }

            return new HttpResponseMessage()
            {
                StatusCode = statusCode,
                Content = responseContent
            };
        }

        // JHM TODO: The day after we go live, remove this crappy hack that only exists to support the old Mobile App
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/PersonalizationEngine/Campaign" )]
        [System.Web.Http.Route( "api/PersonalizationEngine/DefaultCampaign" )]
        [Authenticate, Secured]
        public HttpResponseMessage GetDefaultCampaign( string campaignTypeList, int numCampaigns = 1 )
        {
            // now grab a default campaign
            var defaultCampaigns = new List<Campaign>();
            var defaultCampaign = new Campaign
            {
                Name = "CTA Missions Trip",
                Description = "Marketed to people who simply have a login and don't fit other campaigns.",
                StartDate = DateTime.Parse( "1982-09-28T00:00:00" ),
                EndDate = null,
                Type = "WebsiteCard,MobileAppNewsFeed",
                Priority = 0,
                ContentJson = "{\"WebsiteCard\":{\"title\":\"Missions\",\"sub-title\":\"Go on a Trip\",\"body\":\"Get involved in God’s global mission! Sign-up for a short-term trip and make an impact in the lives of others around the world.\",\"img\":\"/Content/ccv.church/pe/dashboard-card/mission-trip-2.jpg\",\"link\":\"/ministries/missions/trips?promo_name=mission-trips&promo_id=12&promo_creative=next-step-go-on-a-trip&promo_position=dashboard-card\",\"link-text\":\"Go on a trip\"},\"MobileAppNewsFeed\":{\"title\":\"Missions\",\"body\":\"\",\"img\":\"/Content/ccv.church/pe/mobile-app/Missions3_1242x801_Newsfeed.jpg\",\"link\":\"/ministries/missions/trips?promo_name=mission-trips&promo_id=12&promo_creative=next-step-go-on-a-trip&promo_position=mobile-app\",\"skip-details-page\":\"true\"}}",
                CreatedDateTime = null,
                ModifiedDateTime = DateTime.Parse( "2019-09-11T15:33:39.347" ),
                CreatedByPersonAliasId = null,
                ModifiedByPersonAliasId = 514673,
                ModifiedAuditValuesAlreadyUpdated = false,
                Attributes = null,
                AttributeValues = null,
                Id = 12,
                Guid = Guid.Parse( "1214592d-4e49-4f61-a607-2d307184973a" ),
                ForeignId = null,
                ForeignGuid = null,
                ForeignKey = null
            };
            defaultCampaigns.Add( defaultCampaign );

            // now, if we've got a result, return it. Otherwise throw an error
            HttpStatusCode statusCode = HttpStatusCode.OK;
            StringContent responseContent = null;

            if ( defaultCampaigns.Count > 0 )
            {
                responseContent = new StringContent( JsonConvert.SerializeObject( defaultCampaigns ), Encoding.UTF8, "application/json" );
            }
            else
            {
                statusCode = HttpStatusCode.NotFound;
                responseContent = new StringContent( string.Format( "Could not find any campaigns for types of: {0}", campaignTypeList ) );
            }

            return new HttpResponseMessage()
            {
                StatusCode = statusCode,
                Content = responseContent
            };
        }
    }
}
