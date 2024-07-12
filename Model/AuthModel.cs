using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace I3S_API.Model
{
    public class WKESSOModel
    {
        public string grant_type { get; set; }
        public string grantCode { get; set; }
        public string redirecturi { get; set; }
    }

    public class WKESSOToken
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }

    }

    public class GoogleToken
    {
        public string grant_type { get; set; }
        public string code { get; set; }
        public string redirect_uri { get; set; }
        public string client_id { get; set; }
        public string client_secret { get; set; }
    }
    public class GoogleUserInfo
    {
        public string id { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string picture { get; set; }
    }

    public class LocalLogin
    {
        public string account { get; set; }
        public string pwd { get; set; }
		public string captcha { get; set; }
	}

    public class WKESSOMemberInfoModel
    {
        public WKESSODataInfo data { get; set; }
    }
    public class WKESSODataInfo
    {
        public int mid { get; set; }
        public string account { get; set; }
        public string nickName { get; set; }
        public string email { get; set; }
    }

    public class SignInSSO
    {
        public string uid { get; set; }
        public string account { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string site { get; set; }
        public string picture { get; set; }
        public int sid { get; set; }
    }

    public class myUnauthorizedResult : JsonResult
    {
        public myUnauthorizedResult(string message2, int StatusCode2 = 401, bool status2 = false) : base(new ResponseModel(message2, StatusCode2, status2))
        {
            StatusCode = StatusCodes.Status401Unauthorized;

        }
    }
}
