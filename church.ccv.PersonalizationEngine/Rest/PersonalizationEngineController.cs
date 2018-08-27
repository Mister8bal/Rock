using church.ccv.PersonalizationEngine.Data;
using Newtonsoft.Json;
using Rock.Rest.Filters;
using System;
using System.Net.Http;
using System.Text;
using static church.ccv.PersonalizationEngine.Model.Campaign;

namespace church.ccv.PersonalizationEngine.Rest
{
    public class PersonalizationEngineController : Rock.Rest.ApiControllerBase
    {
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/PersonalizationEngine/RelevantCampaign" )]
        [Authenticate, Secured]
        public HttpResponseMessage GetRelevantCampaign( string campaignTypeList, int personId )
        {
            // convert the strings to enum
            CampaignType[] campaignTypeEnumList = StringToEnum( campaignTypeList );

            // now request the relevant campaign for the person
            var campaignResult = PersonalizationEngineUtil.GetRelevantCampaign( campaignTypeEnumList, personId );

            // and if nothing was found, take a default
            if( campaignResult == null )
            {
                var defaultCampaigns = PersonalizationEngineUtil.GetDefaultCampaigns( campaignTypeEnumList );
                campaignResult = defaultCampaigns[ 0 ];
            }

            StringContent restContent = new StringContent( JsonConvert.SerializeObject( campaignResult ), Encoding.UTF8, "application/json" );
            return new HttpResponseMessage()
            {
                Content = restContent
            };
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/PersonalizationEngine/DefaultCampaign" )]
        [Authenticate, Secured]
        public HttpResponseMessage GetDefaultCampaign( string campaignTypeList )
        {
            // convert the strings to enum
            CampaignType[] campaignTypeEnumList = StringToEnum( campaignTypeList );

            // now grab a default campaign
            var defaultCampaigns = PersonalizationEngineUtil.GetDefaultCampaigns( campaignTypeEnumList );
            var campaignResult = defaultCampaigns[ 0 ];

            StringContent restContent = new StringContent( JsonConvert.SerializeObject( campaignResult ), Encoding.UTF8, "application/json" );
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
