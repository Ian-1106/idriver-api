using I3S_API.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using static System.Net.WebRequestMethods;

namespace I3S_API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class MapsController : ControllerBase
    {
        /// <summary>
        /// 呼叫TGOS的API實現地址查詢經緯度的功能
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchAddress(string address)
        {
            //https://map.tgos.tw/TGOSCloudMap
            //台中市大里區中興路一段80號


            string BaseURL = $"https://www.google.com/maps/place?q={address}";

            var Result = await ApiHelper.GetApiResponse(BaseURL);
            if (Result is OkObjectResult okResult)
            {
                string htmlContent = okResult.Value.ToString();
                int index = htmlContent.IndexOf(";window.APP_INITIALIZATION_STATE");
                var results = new List<Dictionary<string, object>>();
                if (index != -1)
                {
                    string substring = htmlContent.Substring(index + 36, 49);
                    string[] parts = substring.Split(',');
                    double latitude = double.Parse(parts[1]);
                    double longitude = double.Parse(parts[2]);

                    results.Add(new Dictionary<string, object>
                    {
                        { "address", address },
                        { "x", longitude },
                        { "y", latitude }
                    });
                    return Ok(results);
                }
                return NotFound();
            }
            return NotFound();
        }

    }
}
