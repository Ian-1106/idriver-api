using Microsoft.AspNetCore.Mvc;
using I3S_API.Lib;
using Dapper;
using I3S_API.Model;
using I3S_API.Filter;

namespace I3S_API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemberController : ControllerBase
    {
        /// <summary>
        /// 取得我的會員資訊
        /// </summary>
        [HttpGet("")]
        public IActionResult GetMember()
        {
            int mid = (int)this.HttpContext.Items["MID"];

            string strsql = @"select mid, name, account, img, email, sso, convert(varchar, LastLoginDT, 120) as lastLoginDT, classID 
                            from vs_member where mid = @mid";

            using (var db = new AppDb())
            {

                var data = db.Connection.QueryFirstOrDefault(strsql, new { mid });
                return Ok(data);
            }
        }

        /// <summary>
        /// 紀錄用戶所有紀錄
        /// </summary>
        [HttpPost("Record")]
        public IActionResult AddMemberSearchRecord(int mid, string type, string record)
        {
            string strsql = @"exec xp_insertMemberRecord @mid, @type, @record";
            using (var db = new AppDb())
            {

                var data = db.Connection.QueryFirstOrDefault(strsql, new { mid, type, record });
                if (data == null)
                {
                    return NoContent();
                }
                return Ok(new { message = data.Message });
            }
        }
    }
}
