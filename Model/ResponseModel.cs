using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace I3S_API.Model
{
    public class ResponseModel
    {
        public string message { get; set; }
        public int statusCode { get; set; }
        public bool status { get; set; }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
        public ResponseModel(string message2 = "API發生錯誤",int statusCode2 = 500, bool status2 = false)
        {
            message = message2;
            statusCode = statusCode2;
            status = status2;
        }
    }
}
