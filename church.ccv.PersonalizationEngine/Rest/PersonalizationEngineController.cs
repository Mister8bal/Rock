using church.ccv.PersonalizationEngine.Data;
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

            // and if nothing was found, take a default
            if ( campaignResults.Count == 0 )
            {
                var defaultCampaigns = PersonalizationEngineUtil.GetDefaultCampaign( campaignTypeList );

                if ( defaultCampaigns.Count > 0 )
                {
                    campaignResults.Add( defaultCampaigns [ 0 ] );
                }
            }


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

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/PersonalizationEngine/Campaign" )]
        [System.Web.Http.Route( "api/PersonalizationEngine/DefaultCampaign" )]
        [Authenticate, Secured]
        public HttpResponseMessage GetDefaultCampaign( string campaignTypeList, int numCampaigns = 1 )
        {
            // now grab a default campaign
            var defaultCampaigns = PersonalizationEngineUtil.GetDefaultCampaign( campaignTypeList, numCampaigns );


            // now, if we've got a result, return it. Otherwise throw an error
            HttpStatusCode statusCode = HttpStatusCode.OK;
            StringContent responseContent = null;

            if( defaultCampaigns.Count > 0 )
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
