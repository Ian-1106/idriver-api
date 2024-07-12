using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace I3S_API.Helpers
{
    public static class ApiHelper
    {
        public static async Task<IActionResult> GetApiResponse(string targetUrl)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(targetUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        return new OkObjectResult(responseBody);
                    }
                    else
                    {
                        return new StatusCodeResult((int)response.StatusCode);// 錯誤處理
                    }
                }
                catch (HttpRequestException e)
                {
                    return new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);// 網路錯誤處理
                }
            }
        }
    }
}