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
            // convert the string list into an array of enums
            string[] types = campaignTypeList.Split( ',' );

            CampaignType[] campaignTypeEnumList = new CampaignType[types.Length];
            for( int i = 0; i < types.Length; i++ )
            {
                campaignTypeEnumList[ i ] = (CampaignType)Enum.Parse( typeof( CampaignType ), types[ i ], true );
            }

            // now request the relevant campaign for the person
            var campaignResult = PersonalizationEngineUtil.GetRelevantCampaign( campaignTypeEnumList, personId );
            if( campaignResult == null )
            {
                // and if nothign was found, take a default.
                var defaultCampaigns = PersonalizationEngineUtil.GetDefaultCampaigns( campaignTypeEnumList );
                campaignResult = defaultCampaigns[ 0 ];
            }

            StringContent restContent = new StringContent( JsonConvert.SerializeObject( campaignResult ), Encoding.UTF8, "application/json" );
            return new HttpResponseMessage()
            {
                Content = restContent
            };
        }
    }
}
