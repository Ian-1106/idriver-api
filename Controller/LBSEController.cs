using Dapper;
using I3S_API.Helpers;
using I3S_API.Lib;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace I3S_API.Controllers
{
    [Route("api/LBSE")]
    [ApiController]
    public class LBSEController : ControllerBase
    {
        private const string BaseURL = "http://10.22.21.41/api/lbse/usage";

        [HttpGet("nearpoi")]
        public async Task<IActionResult> GetNearPOIAsync(double lat, double lon, int range, string scopes)
        {
            /*
             * 23.97120
             * 120.947916
             * 3
             * 測速照相,即時路況
             */
            string[] scopesArray = scopes.Split(',');

            try
            {
                string TargetUrl = $"{BaseURL}?lat={lat}&lon={lon}&range={range}&key=FB378D11-FA61-4DBC-8EAB-C3F22C9053CA";
                var LBSEResult = await ApiHelper.GetApiResponse(TargetUrl);

                if (LBSEResult is ObjectResult okResult && okResult.Value != null)
                {
                    var valueResult = okResult.Value.ToString();

                    JObject jsonResult = JObject.Parse(valueResult);
                    JArray dataArray = (JArray)jsonResult["data"];
                    var result = new List<dynamic>();


                    foreach (var item in dataArray)
                    {
                        var ID = (int)item["ID"];
                        var Lat = (double)item["Lat"];
                        var Lon = (double)item["Lon"];
                        var OCName = (string)item["OCName"];
                        var jData = JObject.Parse((string)item["jData"]);

                        foreach (var ss in scopesArray)
                        {
                            if (ss.Equals(OCName))
                            {
                                string strsql = "select * from Object where OID = @ID";
                                using (var db = new AppDb())
                                {
                                    var data = db.Connection.QueryFirstOrDefault(strsql, new { ID });
                                    result.Add(data);
                                }
                            }
                        }
                    }

                    return Ok(result);
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                // Consider using ILogger for logging in a real API
                Console.WriteLine($"Exception: {ex.Message}");
                return StatusCode(500, new { error = "An error occurred while processing the request.", details = ex.Message });
            }
        }
    }
}