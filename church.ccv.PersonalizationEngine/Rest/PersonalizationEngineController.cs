using church.ccv.PersonalizationEngine.Data;
using Newtonsoft.Json;
using Rock.Rest.Filters;
using System;
using System.Collections.Generic;
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
            // convert the strings to enum
            CampaignType[] campaignTypeEnumList = StringToEnum( campaignTypeList );

            // now request the relevant campaign for the person
            List<Model.Campaign> campaignResults = PersonalizationEngineUtil.GetRelevantCampaign( campaignTypeEnumList, personId, numCampaigns );

            // and if nothing was found, take a default
            if ( campaignResults.Count == 0 )
            {
                var defaultCampaigns = PersonalizationEngineUtil.GetDefaultCampaign( campaignTypeEnumList );
                campaignResults.Add( defaultCampaigns[ 0 ] );
            }

            StringContent restContent = new StringContent( JsonConvert.SerializeObject( campaignResults ), Encoding.UTF8, "application/json" );
            return new HttpResponseMessage()
            {
                Content = restContent
            };
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/PersonalizationEngine/Campaign" )]
        [System.Web.Http.Route( "api/PersonalizationEngine/DefaultCampaign" )]
        [Authenticate, Secured]
        public HttpResponseMessage GetDefaultCampaign( string campaignTypeList, int numCampaigns = 1 )
        {
            // convert the strings to enum
            CampaignType[] campaignTypeEnumList = StringToEnum( campaignTypeList );

            // now grab a default campaign
            var defaultCampaigns = PersonalizationEngineUtil.GetDefaultCampaign( campaignTypeEnumList, numCampaigns );

            StringContent restContent = new StringContent( JsonConvert.SerializeObject( defaultCampaigns ), Encoding.UTF8, "application/json" );
            return new HttpResponseMessage()
            {
                Content = restContent
            };
        }

        CampaignType[] StringToEnum( string campaignTypeList )
        {
            // convert the string list into an array of enums
            string[] types = campaignTypeList.Split( ',' );

            CampaignType[] campaignTypeEnumList = new CampaignType[types.Length];
            for( int i = 0; i < types.Length; i++ )
            {
                campaignTypeEnumList[ i ] = (CampaignType)Enum.Parse( typeof( CampaignType ), types[ i ], true );
            }

            return campaignTypeEnumList;
        }
    }
}
