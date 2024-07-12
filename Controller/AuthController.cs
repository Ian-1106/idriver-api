using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Web;
using I3S_API.Lib;
using I3S_API.Model;
using System.Dynamic;
using Microsoft.IdentityModel.Tokens;

namespace I3S_API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        /// <summary>
        /// state為導回網站網址
        /// </summary>
        [HttpGet("ssologin")]
        //[ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult SSOLogin(string? state, string sso)
        {
            string client_id;
            string baseaddress;
            string redirecturi;
            string weburi = AppConfig.Config["Weburi"];

            string redirectto;
            state = HttpUtility.UrlEncode(state);

            switch (sso)
            {
                case ("wkesso"):
                    client_id = AppConfig.Config["WKESSO:ClientID"];
                    redirecturi = AppConfig.Config["WKESSO:RedirectUri"];
                    baseaddress = AppConfig.Config["WKESSO:BaseAddress"];

                    redirectto = $"{baseaddress}/loginpage?client_id={client_id}&redirecturi={redirecturi}{(state == null ? "" : "&state=" + state)}";

                    return Redirect(redirectto);
                case ("google"):
                    client_id = AppConfig.Config["GoogleSSO:client_id"];
                    redirecturi = AppConfig.Config["GoogleSSO:redirect_uri"];
                    baseaddress = AppConfig.Config["GoogleSSO:auth_uri"];

                    redirectto = $"{baseaddress}?scope=openid https://www.googleapis.com/auth/userinfo.profile https://www.googleapis.com/auth/userinfo.email" +
                                 $"&response_type=code&redirect_uri={redirecturi}&client_id={client_id}{(state == null ? "" : "&state=" + state)}";

                    return Redirect(redirectto);
                default:
                    return Ok();
            }
        }

        /// <summary>
        /// 本地登入
        /// </summary>
        [HttpPost("login")]
        public IActionResult Login([FromBody] LocalLogin body)
        {
			// 創建 Fn 實例
			Fn Fn = new Fn();

			// 從 HttpContext 中獲取 MID 和 SID
			int mid = (int)this.HttpContext.Items["MID"];
			int sid = (int)this.HttpContext.Items["SID"];

			// 初始化消息和狀態
			string message = "";
			bool status = false;

			// 讀取 JSON 檔案並獲取驗證碼金鑰
			string Captcha_Key = AppConfig.Config[$"Captcha_Key:Signin"];

			// 從請求的 Body 中獲取名為 "captcha" 的值
			string CaptchaVal_Input = body.captcha;

			// 使用 MD5 加密輸入的驗證碼
			string CaptchaVal_MD5Result = Fn.MD5Hash(CaptchaVal_Input + Captcha_Key);

			// 從請求的 Cookie 中獲取名為 "Signin" 的驗證碼
			string CaptchaVal_Cookie = Request.Cookies["Signin"];

			// 檢查驗證碼是否存在且是否正確
			bool IsCaptchaExist = string.IsNullOrEmpty(CaptchaVal_Cookie);
			bool IsCaptchaMatch = CaptchaVal_MD5Result == CaptchaVal_Cookie;

			// 如果驗證碼不存在，回傳錯誤訊息
			if (IsCaptchaExist || !IsCaptchaMatch)
			{
				message = IsCaptchaExist ? "驗證碼過期" : "驗證碼錯誤";
				return Ok(new { message, status });
			}

			// 如果 MID 大於 0，表示用戶已登入，則回傳請先登出的錯誤訊息
			if (mid > 0)
			{
				message = "請先登出";
				return Ok(new { message, status });
			}


			string strSql = "xp_signInLocal";

            // 紀錄Log
            this.HttpContext.Items.Add("ObjectName", strSql);

            using (var db = new AppDb())
            {
                var p = new DynamicParameters();
                p.Add("@account", body.account);
                p.Add("@pwd", body.pwd);
                p.Add("@sid", sid);
                p.Add("@status", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                p.Add("@message", dbType: DbType.String, direction: ParameterDirection.Output, size: 255);
                db.Connection.Execute(strSql, p, commandType: CommandType.StoredProcedure);
                status = p.Get<bool>("@status");
                message = p.Get<string>("@message");

                // 紀錄Log
                this.HttpContext.Items.Add("SP_InOut", p);

                return Ok(new { message, status });
            }
        }


        /// <summary>
        /// callback
        /// </summary>
        [HttpGet("wkesso")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult WKESSO(string? state, string grantCode)
        {
            int sid = (int)this.HttpContext.Items["SID"];

            string client_id = AppConfig.Config["WKESSO:ClientID"];
            string redirecturi = AppConfig.Config["WKESSO:RedirectUri"];
            string baseaddress = AppConfig.Config["WKESSO:BaseAddress"];
            string client_secret = AppConfig.Config["WKESSO:ClientSecret"];
            string site = AppConfig.Config["WKESSO:Site"];
            string Weburi = AppConfig.Config["Weburi"];

            Fn fn = new Fn();

            string basic = fn.Base64Encode($"{client_id}:{client_secret}");

            WKESSOModel data = new WKESSOModel
            {
                grantCode = grantCode,
                redirecturi = redirecturi,
                grant_type = "authorization_code"
            };

            WKESSOToken result = fn.Post<WKESSOToken>("api/Client/token ", basic, data, baseaddress, out bool requestApiResult);

            if (requestApiResult)
            {
                dynamic getRequest = new ExpandoObject();
                getRequest.bearerToken = result.access_token;

                string url = $"{baseaddress}/api/Member/resource";
                WKESSOMemberInfoModel memberinfo = fn.Get<WKESSOMemberInfoModel>(url, getRequest, out bool MemberResult1);


                if (MemberResult1)
                {
                    SignInSSO signIn = new SignInSSO
                    {
                        uid =  memberinfo.data.mid.ToString(),
                        name =  memberinfo.data.nickName,
                        account = memberinfo.data.account,
                        email = memberinfo.data.email,
                        site = site,
                        picture = null,
                        sid = sid
                    };

                    Uri uri = fn.signInSSO(signIn, Weburi, state, sid);
                    return Redirect(uri.AbsoluteUri);
                }
            }

            return Redirect(Weburi);

        }

        /// <summary>
        /// callback
        /// </summary>
        [HttpGet("google")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Google(string? state, string? code)
        {
            if (code.IsNullOrEmpty())
            {
                ResponseModel responseModel = new ResponseModel("授權錯誤", 401, false);
                return Ok(responseModel);
            }

            int sid = (int)this.HttpContext.Items["SID"];

            string token_base = AppConfig.Config["GoogleSSO:token_base"];
            string token_cmd = AppConfig.Config["GoogleSSO:token_cmd"];
            string redirecturi = AppConfig.Config["GoogleSSO:redirect_uri"];
            string client_id = AppConfig.Config["GoogleSSO:client_id"];
            string client_secret = AppConfig.Config["GoogleSSO:client_secret"];
            string site = AppConfig.Config["GoogleSSO:site"];
            string Weburi = AppConfig.Config["Weburi"];

            Fn fn = new Fn();

            GoogleToken data = new GoogleToken
            {
                grant_type = "authorization_code",
                code = code,
                redirect_uri = redirecturi,
                client_id = client_id,
                client_secret = client_secret
            };

            dynamic result = fn.Post<dynamic>(token_cmd, "", data, token_base, out bool requestApiResult);

            if (requestApiResult)
            {
                string access_token = result.access_token;
                string googleUserInfoAPI = AppConfig.Config["GoogleSSO:googleUserInfoAPI"];
                string url = $"{googleUserInfoAPI}{access_token}";

                dynamic getRequest = new ExpandoObject();
                getRequest.bearerToken = access_token;

                GoogleUserInfo userInfo = fn.Get<GoogleUserInfo>(url, getRequest, out bool userInfoResult);
                if (userInfoResult)
                {
                    SignInSSO signIn = new SignInSSO
                    {
                        uid = userInfo.id,
                        name = userInfo.name,
                        account = userInfo.name,
                        email = userInfo.email,
                        site = site,
                        picture = userInfo.picture,
                        sid = sid
                    };

                    Uri uri = fn.signInSSO(signIn, Weburi, state, sid);
                    return Redirect(uri.AbsoluteUri);
                }

            }

            return Redirect(Weburi);

        }

        /// <summary>
        /// 登出
        /// </summary>
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            int sid = (int)this.HttpContext.Items["SID"];
            int mid = (int)this.HttpContext.Items["MID"];

            string message = "已登出";
            bool status = true;

            if(mid != 0)
            {
                string strSql = "xp_signOut";

                // 紀錄Log
                this.HttpContext.Items.Add("ObjectName", strSql);

                using (var db = new AppDb())
                {
                    var p = new DynamicParameters();
                    p.Add("@sid", sid);
                    db.Connection.Execute(strSql, p, commandType: CommandType.StoredProcedure);

                    // 紀錄Log
                    this.HttpContext.Items.Add("SP_InOut", p);
                }
            }

            return Ok(new { status, message });
        }


    }
}
