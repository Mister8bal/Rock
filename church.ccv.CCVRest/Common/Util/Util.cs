using System.Net;
using System.Net.Http;
using System.Text;
using church.ccv.CCVRest.Common.Model;
using Newtonsoft.Json;

namespace church.ccv.CCVRest.Common
{
    public class Util
    {
        // Utility function for handling the response model
        public static HttpResponseMessage GenerateResponse( bool success, string message, object data )
        {
            ResponseModel response = new ResponseModel
            {
                Success = success,
                Message = message,
                Data = data
            };

            StringContent restContent = new StringContent( JsonConvert.SerializeObject( response ), Encoding.UTF8, "application/json" );
            return new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = restContent
            };
        }
    }
}
