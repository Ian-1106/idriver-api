using Dapper;
using I3S_API.Helpers;
using I3S_API.Lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace I3S_API.Controllers
{
    [Route("api/LBSE")]
    [ApiController]
    public class LBSEController : ControllerBase
    {
        private const string BaseURL = "http://10.22.21.41/api/lbse";
        private readonly ILogger<LBSEController> _logger;

        public LBSEController(ILogger<LBSEController> logger)
        {
            _logger = logger;
        }

        [HttpGet("nearpoi")]
        public async Task<IActionResult> GetNearPOIAsync(double lat, double lon, int range, string scopes)
        {
            /*
             * 23.97120
             * 120.947916
             * 3
             * 測速照相
             */

            string[] scopesArray = scopes.Split(',');

            try
            {
                string TargetUrl = $"{BaseURL}/usage?lat={lat}&lon={lon}&range={range}&key=FB378D11-FA61-4DBC-8EAB-C3F22C9053CA";
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
                                    if (data != null)
                                    {
                                        result.Add(data);
                                    }
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
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, new { error = "An error occurred while processing the request.", details = ex.Message });
            }
        }

        [HttpGet("weather")]
        public async Task<IActionResult> GetWeatherAsync(double lat, double lon)
        {
            /*
             * 23.97120
             * 120.947916
             */
            try
            {
                string TargetUrl = $"{BaseURL}/public?lat={lat}&lon={lon}&key=FB378D11-FA61-4DBC-8EAB-C3F22C9053CA";
                var LBSEResult = await ApiHelper.GetApiResponse(TargetUrl);

                if (LBSEResult is ObjectResult okResult && okResult.Value != null)
                {
                    var valueResult = okResult.Value.ToString();
                    JObject jsonResult = JObject.Parse(valueResult);
                    var path = jsonResult["data"]?["path"]?.ToString();

                    if (!string.IsNullOrEmpty(path))
                    {
                        var pathParts = path.Split('/');
                        if (pathParts.Length >= 4)
                        {
                            var city = pathParts[2];
                            var town = pathParts[3];

                            string WeatherUrl = $"https://opendata.cwa.gov.tw/api/v1/rest/datastore/O-A0001-001?Authorization=CWA-E5231FF2-8050-47A1-9733-C2BE8E0FD1E0";
                            var weatherResult = await ApiHelper.GetApiResponse(WeatherUrl);

                            if (weatherResult is ObjectResult okResult2 && okResult2.Value != null)
                            {
                                var valueResult2 = okResult2.Value.ToString();
                                JObject jsonResult2 = JObject.Parse(valueResult2);
                                var stations = jsonResult2["records"]?["Station"]?.ToObject<List<JObject>>();

                                foreach (var station in stations)
                                {
                                    string countryName = station["GeoInfo"]?["CountyName"]?.ToString();
                                    string townName = station["GeoInfo"]?["TownName"]?.ToString();

                                    if (city.Equals(countryName) && town.Equals(townName))
                                    {
                                        var stationJson = JObject.FromObject(station);
                                        _logger.LogInformation("Returning JSON response: {0}", stationJson.ToString());

                                        var responseJson = JsonConvert.SerializeObject(new { city, town, stationJson });
                                        return Content(responseJson, "application/json");
                                    }
                                }
                            }
                        }
                    }
                }
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, new { error = "An error occurred while processing the request.", details = ex.Message });
            }
        }
    }
}
