using Azure;
using Dapper;
using I3S_API.Helpers;
using I3S_API.Lib;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var scopesArray = scopes.Split(',');
            var weatherResult = (await GetWeatherData(lat, lon)) ?? new WeatherResponse();
            var poisResult = await GetPOIData(lat, lon, range, scopesArray);

            var cell = new CombinedResponse
            {
                Weather = weatherResult,
                POIs = poisResult
            };

            var jsonResponse = JsonConvert.SerializeObject(cell);
            return new ContentResult
            {
                Content = jsonResponse,
                ContentType = "application/json",
                StatusCode = 200
            };
        }

        private async Task<WeatherResponse> GetWeatherData(double lat, double lon)
        {
            try
            {
                string targetUrl = $"{BaseURL}/public?lat={lat}&lon={lon}&key=FB378D11-FA61-4DBC-8EAB-C3F22C9053CA";
                var lbsResult = await ApiHelper.GetApiResponse(targetUrl);

                if (lbsResult is ObjectResult okResult && okResult.Value != null)
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

                            string weatherUrl = $"https://opendata.cwa.gov.tw/api/v1/rest/datastore/O-A0001-001?Authorization=CWA-E5231FF2-8050-47A1-9733-C2BE8E0FD1E0";
                            var result = await ApiHelper.GetApiResponse(weatherUrl);

                            if (result is ObjectResult okResult2 && okResult2.Value != null)
                            {
                                var valueResult2 = okResult2.Value.ToString();
                                JObject jsonResult2 = JObject.Parse(valueResult2);
                                var stations = jsonResult2["records"]?["Station"]?.ToObject<List<JObject>>();

                                foreach (var station in stations)
                                {
                                    var countryName = station["GeoInfo"]?["CountyName"]?.ToString();
                                    var townName = station["GeoInfo"]?["TownName"]?.ToString();

                                    if (city.Equals(countryName, StringComparison.OrdinalIgnoreCase) &&
                                        town.Equals(townName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        var stationJson = JObject.FromObject(station);
                                        return new WeatherResponse
                                        {
                                            City = city,
                                            Town = town,
                                            StationJson = stationJson
                                        };
                                    }
                                }
                            }
                        }
                    }
                }
                return new WeatherResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the weather request.");
                throw;
            }
        }

        private async Task<List<POI>> GetPOIData(double lat, double lon, int range, string[] scopesArray)
        {
            try
            {
                string targetUrl = $"{BaseURL}/usage?lat={lat}&lon={lon}&range={range}&key=FB378D11-FA61-4DBC-8EAB-C3F22C9053CA";
                var lbsResult = await ApiHelper.GetApiResponse(targetUrl);

                if (lbsResult is ObjectResult okResult && okResult.Value != null)
                {
                    var valueResult = okResult.Value.ToString();
                    JObject jsonResult = JObject.Parse(valueResult);
                    JArray dataArray = (JArray)jsonResult["data"];
                    var result = new List<POI>();

                    foreach (var item in dataArray)
                    {
                        var id = (int)item["ID"];
                        var ocName = (string)item["OCName"];

                        if (scopesArray.Contains(ocName, StringComparer.OrdinalIgnoreCase))
                        {
                            string strSql = "SELECT * FROM Object WHERE OID = @ID";
                            using (var db = new AppDb())
                            {
                                var data = db.Connection.QueryFirstOrDefault<POI>(strSql, new { ID = id });
                                if (data != null)
                                {
                                    result.Add(data);
                                }
                            }
                        }
                    }

                    return result;
                }
                return new List<POI>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the POI request.");
                throw;
            }
        }
    }
}
