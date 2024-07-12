using I3S_API.Lib;
using I3S_API.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.Security.Cryptography;
using System.Security;
using I3S_API.Filter;
using I3S_API.Model;
using System.Text.Json.Nodes;

namespace I3S_API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class TxController : ControllerBase
    {
        /// <summary>
        ///傳入method；first代表取一個；若要fetch則需傳start，counts，first強制變為false；order例：以第一欄未做排序傳"1_a" or "1_d" a、d代表asc，desc
        /// </summary>
        [HttpGet("{uuid}")]
        [UUID2TxViewAuthFilter]
        public IActionResult Method([FromQuery] UUIDAPI req)
        {
            int mid = (int)this.HttpContext.Items["MID"];
            int? cid = (int?)this.HttpContext.Items["CID"];
            int permissionPos = (int)this.HttpContext.Items["PermissionPos"];
            PermissionModel permissionModel = (PermissionModel)this.HttpContext.Items["PermissionModel"];
            dynamic uuid_data = (dynamic)this.HttpContext.Items["UUID_data"];
            bool midmode = (bool)this.HttpContext.Items["midmode"];
            bool txType = (bool)this.HttpContext.Items["TxType"];
            string objectName = this.HttpContext.Items["ObjectName"].ToString();
            int sid = (int)this.HttpContext.Items["SID"];

            UUID fn_uuid = new UUID();

            string sqlstring, strsqltotal;
            DynamicParameters p = new DynamicParameters();
            bool first = req.first;

            // xp select
            if (txType)
            {
                TxView txView = fn_uuid.UUIDTxViewParam(objectName, uuid_data, sid, mid, HttpContext);
                sqlstring = txView.sqlstring;
                strsqltotal = txView.sqlstring;
                p = txView.p;
            }
            // view select
            else
            {
                SqlStrModel sqlStrModel = fn_uuid.getSqlString(true, uuid_data, HttpContext, req, permissionModel, mid, cid, permissionPos, midmode, objectName);
                sqlstring = sqlStrModel.strsqlview;
                p = sqlStrModel.p;
                first = sqlStrModel.first;
                strsqltotal = sqlStrModel.strsqltotal;

            }

            using (var db = new AppDb())
            {

                if (first)
                {
                    var data = db.Connection.QueryFirstOrDefault(sqlstring, p);
                    return Ok(data ?? new { });
                }
                else
                {

                    if (req.bTotal)
                    {

                        var rep_totle = db.Connection.QueryFirstOrDefault(strsqltotal, p);

                        // 如果是xp view
                        if (txType)
                            p.Add("@bTotal", null, DbType.Boolean);

                        var data = db.Connection.Query(sqlstring, p);

                        return Ok(new { rep_totle.total, data });
                    }
                    else
                    {
                        var data = db.Connection.Query(sqlstring, p);
                        return Ok(data);
                    }

                }
            }
        }



        /// <summary>
        /// SP
        /// </summary>
        [HttpPost("{uuid}")]
        [UUID2TxSPAuthFilter]
        public IActionResult TxPostMethod([FromBody] TxModel req)
        {
            int mid = (int)this.HttpContext.Items["MID"];
            int sid = (int)this.HttpContext.Items["SID"];
            int? cid = (int?)this.HttpContext.Items["CID"];
            bool midmode = (bool)this.HttpContext.Items["midmode"];
            dynamic uuid_data = (dynamic)this.HttpContext.Items["UUID_data"];
            dynamic bodyParam = (dynamic)this.HttpContext.Items["bodyParam"];

            UUID fn_uuid = new UUID();

            TxInOut txInOut = fn_uuid.UUIDSP_Param(uuid_data, sid, mid, cid, midmode, bodyParam);
            using (var db = new AppDb())
            {
                string sp = uuid_data.Name;
                db.Connection.Execute(sp, txInOut.p, commandType: CommandType.StoredProcedure);

                JsonObject json = fn_uuid.getSPoutput(txInOut.outputs, txInOut.p);

                this.HttpContext.Items.Add("SP_InOut", txInOut.p);

                return Ok(json);
            }

        }

        /// <summary>
        /// SP
        /// </summary>
        [HttpPut("{uuid}")]
        [UUID2TxSPAuthFilter]
        public IActionResult TxPutMethod([FromBody] TxModel req)
        {
            int mid = (int)this.HttpContext.Items["MID"];
            int sid = (int)this.HttpContext.Items["SID"];
            int? cid = (int?)this.HttpContext.Items["CID"];
            bool midmode = (bool)this.HttpContext.Items["midmode"];
            dynamic uuid_data = (dynamic)this.HttpContext.Items["UUID_data"];
            dynamic bodyParam = (dynamic)this.HttpContext.Items["bodyParam"];

            UUID fn_uuid = new UUID();

            TxInOut txInOut = fn_uuid.UUIDSP_Param(uuid_data, sid, mid, cid, midmode, bodyParam);
            using (var db = new AppDb())
            {
                string sp = uuid_data.Name;
                db.Connection.Execute(sp, txInOut.p, commandType: CommandType.StoredProcedure);

                JsonObject json = fn_uuid.getSPoutput(txInOut.outputs, txInOut.p);

                this.HttpContext.Items.Add("SP_InOut", txInOut.p);

                return Ok(json);
            }

        }

        /// <summary>
        /// SP
        /// </summary>
        [HttpDelete("{uuid}")]
        [UUID2TxSPAuthFilter]
        public IActionResult TxDeleteMethod([FromBody] TxModel req)
        {
            int mid = (int)this.HttpContext.Items["MID"];
            int sid = (int)this.HttpContext.Items["SID"];
            int? cid = (int?)this.HttpContext.Items["CID"];
            bool midmode = (bool)this.HttpContext.Items["midmode"];
            dynamic uuid_data = (dynamic)this.HttpContext.Items["UUID_data"];
            dynamic bodyParam = (dynamic)this.HttpContext.Items["bodyParam"];

            UUID fn_uuid = new UUID();

            TxInOut txInOut = fn_uuid.UUIDSP_Param(uuid_data, sid, mid, cid, midmode, bodyParam);
            using (var db = new AppDb())
            {
                string sp = uuid_data.Name;
                db.Connection.Execute(sp, txInOut.p, commandType: CommandType.StoredProcedure);

                JsonObject json = fn_uuid.getSPoutput(txInOut.outputs, txInOut.p);

                this.HttpContext.Items.Add("SP_InOut", txInOut.p);

                return Ok(json);
            }

        }
    }
}
