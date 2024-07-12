using I3S_API.Lib;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using Dapper;
using I3S_API.Model;
using Microsoft.IdentityModel.Tokens;
using System.Runtime.CompilerServices;
using I3S_API.Filter;

namespace I3S_API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class PublicController : ControllerBase
    {
        /// <summary>
        ///傳入method；first代表取一個；若要fetch則需傳start，counts，first強制變為false；order例：以第一欄未做排序傳"1_a" or "1_d" a、d代表asc，desc
        /// </summary>
        [HttpGet("{uuid}")]
        [UUIDAuthFilter]
        public IActionResult Method([FromQuery] UUIDAPI req)
        {
            int mid = (int)this.HttpContext.Items["MID"];
            int? cid = (int?)this.HttpContext.Items["CID"];
            bool bPermission = (bool)this.HttpContext.Items["bPermission"];
            int permissionPos = (int)this.HttpContext.Items["PermissionPos"];
            PermissionModel permissionModel = (PermissionModel)this.HttpContext.Items["PermissionModel"];
            dynamic uuid_data = (dynamic)this.HttpContext.Items["UUID_data"];
            string objectName = this.HttpContext.Items["ObjectName"].ToString();

            using (var db = new AppDb(bPermission ? "Default" : "PublicRead"))
            {
                UUID fn_uuid = new UUID();

                SqlStrModel sqlStrModel = fn_uuid.getSqlString(bPermission, uuid_data, HttpContext, req, permissionModel, mid, cid, permissionPos, false, objectName);

                if (sqlStrModel.first)
                {
                    var data = db.Connection.QueryFirstOrDefault(sqlStrModel.strsqlview, sqlStrModel.p);
                    return Ok(data ?? new { });
                }
                else
                {
                    var data = db.Connection.Query(sqlStrModel.strsqlview, sqlStrModel.p);

                    if (req.bTotal)
                    {
                        var rep_totle = db.Connection.QueryFirstOrDefault(sqlStrModel.strsqltotal, sqlStrModel.p);

                        return Ok(new { rep_totle.total, data });
                    }
                    else
                    {
                        return Ok(data);
                    }

                }

            }

        }
    }
}
