using Dapper;
using I3S_API.Model;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Data;
using System.Text.Json.Nodes;
using I3S_API.Model;

namespace I3S_API.Lib
{
    public class UUID
    {
        public List<OrderListModel> normalizeOrderstr(string order)
        {
            if (order.IsNullOrEmpty())
                return null;

            string[] ordersplit = order.Split(",");

            List<OrderListModel> orderList = new List<OrderListModel>();

            bool allow = true;
            for (int i = 0; i< ordersplit.Length; i++)
            {
                string str = ordersplit[i];
                string[] item = str.Split("_");

                //長度不等於2且不是a或d
                if (item.Length != 2 || !(item[1] == "a" || item[1] == "d"))
                {
                    allow = false;
                    break;
                }

                string column = item[0];
                string ordertype = item[1] == "a" ? "asc" : "desc";

                orderList.Add(new OrderListModel()
                {
                    index = i,
                    column = column,
                    ordertype = ordertype,
                    orderstr = $"[{column}] {ordertype}"
                });
            }

            return allow ? orderList : null;
        }

        public PublicModel getSqlandParam(HttpContext context, dynamic columnjson, int mid, string order, string like_column, string like, int? likeMode)
        {
            var param = context.Request.Query;

            int l = columnjson.Count;
            var p = new DynamicParameters();

            string where = @"";
            string ordersql = "";

            List<OrderListModel> orderList = normalizeOrderstr(order);
            List<OrderListModel> correctOrderList = new List<OrderListModel>();

            bool border = orderList.IsNullOrEmpty() ? false : true;

            string liketstr = "";

            Param fn_param = new Param();

            for (int i = 0; i < l; i++)
            {
                //取得欄位名稱與型態
                string name = columnjson[i].name;
                string type = columnjson[i].type;

                if (name == "mid" || name == "sid")
                {
                    continue;
                }

                //比對order
                if (border && !orderList.IsNullOrEmpty())
                {
                    for (int j = 0; j < orderList.Count; j++)
                    {
                        if (orderList[j].column == name)
                        {
                            correctOrderList.Add(orderList[j]);
                            orderList.Remove(orderList[j]);
                        }
                    }
                }

                if (like_column == name && !like.IsNullOrEmpty())
                {
                    string tmp = "";
                    switch (likeMode)
                    {
                        case 0:
                            tmp = "'%' + @like + '%'";
                            break;
                        case 1:
                            tmp = "@like + '%'";
                            break;
                        case 2:
                            tmp = "'%' + @like";
                            break;
                        default:
                            tmp = "'%' + @like + '%'";
                            break;
                    }
                    liketstr = $" [{name}] like " + tmp;
                    p.Add("like", like, DbType.String);
                }


                string? value = param[name];
                if (value == null)
                {
                    continue;
                }
                else
                {
                    where = where + $"{(where == "" ? " where" : " and")} [{name}] = @{name}";


                    switch (type)
                    {
                        case "int":
                            p.Add(name, fn_param.ConvertType<int?>(value), DbType.Int32);
                            break;
                        case "smallint":
                            p.Add(name, fn_param.ConvertType<int?>(value), DbType.Int32);
                            break;
                        case "tinyint":
                            p.Add(name, fn_param.ConvertType<int?>(value), DbType.Int32);
                            break;
                        case "float":
                            p.Add(name, fn_param.ConvertType<double?>(value), DbType.Double);
                            break;
                        case "bit":
                            p.Add(name, fn_param.ConvertType<bool?>(value), DbType.Boolean);
                            break;
                        case "uniqueidentifier":
                            p.Add(name, fn_param.ConvertType<Guid?>(value), DbType.Guid);
                            break;
                        default:
                            p.Add(name, fn_param.ConvertType<string?>(value), DbType.String);
                            break;
                    }


                }

            }

            if (correctOrderList.Count > 0)
            {
                correctOrderList.Sort((x, y) => x.index.CompareTo(y.index));

                correctOrderList.ForEach(v => ordersql += $"{(ordersql == "" ? "order by" : ",")} {v.orderstr}");
            }
            else
            {
                border = false;
            }

            if (liketstr.Length > 0)
                where = (where == "" ? " where " : where + " and ") + liketstr;

            PublicModel response = new PublicModel();
            response.where = where;
            response.p = p;
            response.ordersql = ordersql;
            response.border = border;
            return response;
        }

        public SqlStrModel getSqlString(bool bPermission, dynamic uuid_data, HttpContext context, dynamic req, PermissionModel permissionModel, int mid, int? cid, int permissionPos, bool midmode, string objectName)
        {
            dynamic columnjson = JsonConvert.DeserializeObject(bPermission ? uuid_data.vParam : uuid_data.pvParam);

            PublicModel publicModel = getSqlandParam(context, columnjson, mid, req.order, req.like_column, req.like, req.likeMode);

            //fetch 與 start，counts
            string fetch = "";
            if (req.start != null && req.counts != null && publicModel.border)
            {
                fetch = "offset @start - 1 row fetch next @counts rows only";
                req.first = false;
                publicModel.p.Add("start", req.start, DbType.Int32);
                publicModel.p.Add("counts", req.counts, DbType.Int32);
            }

            string permission = @"";
            if (bPermission && !midmode)
            {
                //狀況一 : 無傳CID需檢查權限
                if (cid == null)
                {
                    permission = $"{(publicModel.where == "" ? " where" : " and")} dbo.fs_checkUserPermission(CID, @mid, @permissionPos) = 1";
                    publicModel.p.Add("mid", mid, DbType.Int32);
                    publicModel.p.Add("permissionPos", permissionPos, DbType.Int32);
                }
                //狀況二 : 有傳CID且CheckObject為true (CheckObject為true一定有傳CID)，檢查是否可以檢視隱藏Object
                else if (uuid_data.CheckObject == true)
                {
                    permission = $"{(publicModel.where == "" ? "where" : "and")} {("iif(hide = 1, @manage, 1) = 1")}";
                    publicModel.p.Add("manage", permissionModel.M, DbType.Boolean);
                }
                //狀況三 : 有傳CID，上方已檢查過權限不需動做
            }
            string topdefault = fetch == "" ? "top 100" : "";

            string midmodesql = @"";
            if (midmode)
            {
                midmodesql = $@"{(publicModel.where.IsNullOrEmpty() ? " where" : " and")} mid = @mid";
                publicModel.p.Add("mid", mid, DbType.Int32);
            }

            string strsqlview = @$"select {(req.top != null ? $"top {req.top}" : topdefault)} * from {objectName} {publicModel.where} {(midmode ? midmodesql : permission)} {(publicModel.border ? publicModel.ordersql : "")} {fetch}";

            string strsqltotal = @$"select count(*) 'total' from {objectName} {publicModel.where} {(midmode ? midmodesql : permission)}";

            SqlStrModel sqlStrModel = new SqlStrModel
            {
                strsqlview = strsqlview,
                strsqltotal = strsqltotal,
                first = req.first,
                p = publicModel.p
            };

            return sqlStrModel;
        }

        public int getPermissionPos(string? permission)
        {
            int pos;
            switch (permission)
            {
                case ("V"):
                    pos = 0;
                    break;
                case ("R"):
                    pos = 1;
                    break;
                case ("I"):
                    pos = 2;
                    break;
                case ("D"):
                    pos = 3;
                    break;
                case ("U"):
                    pos = 4;
                    break;
                case ("M"):
                    pos = 5;
                    break;
                case ("S"):
                    pos = 6;
                    break;
                default:
                    pos = -1;
                    break;
            }
            return pos;
        }
        public myUnauthorizedResult checkCIDPermission(string permission, int mid, int? cid)
        {
            int pos = getPermissionPos(permission);

            if (pos == -1)
            {
                return new myUnauthorizedResult("權限有誤");
            }
            else
            {
                string strsql = @$"select dbo.fs_checkUserPermission(@cid, @mid, @pos) 'permission'";
                using (var db = new AppDb())
                {
                    var check = db.Connection.QueryFirstOrDefault(strsql, new { cid, mid, pos });
                    if (!check.permission)
                    {

                        return new myUnauthorizedResult(mid == 0 ? "無權限，請嘗試重新登入或按F5重新整理網頁" : "無權限");
                    }
                }
            }

            return null;
        }

        public DynamicParameters addType(DynamicParameters p, string declare, string name, dynamic bodyParam, string type)
        {
            Param param = new Param();
            switch (type)
            {
                case "int":
                    p.Add(declare, param.getParamFromBody<int?>(name, bodyParam), DbType.Int32);
                    break;
                case "smallint":
                    p.Add(declare, param.getParamFromBody<int?>(name, bodyParam), DbType.Int32);
                    break;
                case "tinyint":
                    p.Add(declare, param.getParamFromBody<int?>(name, bodyParam), DbType.Int32);
                    break;
                case "float":
                    p.Add(declare, param.getParamFromBody<Double?>(name, bodyParam), DbType.Double);
                    break;
                case "bit":
                    p.Add(declare, param.getParamFromBody<bool?>(name, bodyParam), DbType.Boolean);
                    break;
                case "uniqueidentifier":
                    p.Add(declare, param.getParamFromBody<Guid?>(name, bodyParam), DbType.Guid);
                    break;
                default:
                    p.Add(declare, param.getParamFromBody<string?>(name, bodyParam), DbType.String);
                    break;
            }
            return p;
        }

        public DynamicParameters addTypeQuery(HttpContext httpContext, DynamicParameters p, string declare, string name, string type)
        {
            Param param = new Param();
            switch (type)
            {
                case "int":
                    p.Add(declare, param.getParamFromQuery<int?>(httpContext, name), DbType.Int32);
                    break;
                case "smallint":
                    p.Add(declare, param.getParamFromQuery<int?>(httpContext, name), DbType.Int32);
                    break;
                case "tinyint":
                    p.Add(declare, param.getParamFromQuery<int?>(httpContext, name), DbType.Int32);
                    break;
                case "float":
                    p.Add(declare, param.getParamFromQuery<Double?>(httpContext, name), DbType.Double);
                    break;
                case "bit":
                    p.Add(declare, param.getParamFromQuery<bool?>(httpContext, name), DbType.Boolean);
                    break;
                case "uniqueidentifier":
                    p.Add(declare, param.getParamFromQuery<Guid?>(httpContext, name), DbType.Guid);
                    break;
                default:
                    p.Add(declare, param.getParamFromQuery<string?>(httpContext, name), DbType.String);
                    break;
            }
            return p;
        }

        public TxInOut UUIDSP_Param(dynamic uuid_data, int sid, int mid, int? cid, bool midmode, dynamic bodyParam)
        {
            TxInOut txInOut = new TxInOut();

            txInOut.p = new DynamicParameters();

            txInOut.outputs = new List<dynamic>();

            dynamic columnjson = JsonConvert.DeserializeObject(uuid_data.Param);

            for (int i = 0; i < columnjson.Count; i++)
            {
                //取得欄位名稱與型態
                string declare = columnjson[i].Param;
                string name = declare.Substring(1);

                string type = columnjson[i].Type;

                string mode = columnjson[i].Mode;
                if (mode == "IN")
                {
                    switch (name)
                    {
                        case "mid":
                            txInOut.p.Add(declare, mid, DbType.Int32);
                            break;
                        case "sid":
                            txInOut.p.Add(declare, sid, DbType.Int32);
                            break;
                        default:
                            txInOut.p = addType(txInOut.p, declare, name, bodyParam, type);
                            break;
                    }
                }
                else if (mode == "INOUT")
                {
                    txInOut.outputs.Add(columnjson[i]);
                    switch (type)
                    {
                        case "int":
                            txInOut.p.Add(declare, dbType: DbType.Int32, direction: ParameterDirection.Output);
                            break;
                        case "smallint":
                            txInOut.p.Add(declare, dbType: DbType.Int32, direction: ParameterDirection.Output);
                            break;
                        case "tinyint":
                            txInOut.p.Add(declare, dbType: DbType.Int32, direction: ParameterDirection.Output);
                            break;
                        case "float":
                            txInOut.p.Add(declare, dbType: DbType.Double, direction: ParameterDirection.Output);
                            break;
                        case "bit":
                            txInOut.p.Add(declare, dbType: DbType.Boolean, direction: ParameterDirection.Output);
                            break;
                        case "uniqueidentifier":
                            txInOut.p.Add(declare, dbType: DbType.Guid, direction: ParameterDirection.Output);
                            break;
                        default:
                            txInOut.p.Add(declare, dbType: DbType.String, direction: ParameterDirection.Output, size: 255);
                            break;
                    }
                }
            }
            txInOut.columnjson = columnjson;

            return txInOut;
        }
        public TxView UUIDTxViewParam(string sp_name, dynamic uuid_data, int sid, int mid, HttpContext httpContext)
        {
            TxView txView = new TxView();

            txView.p = new DynamicParameters();

            txView.sqlstring = $"exec [{sp_name}]";

            dynamic columnjson = JsonConvert.DeserializeObject(uuid_data.vParam);

            for (int i = 0; i < columnjson.Count; i++)
            {
                //取得欄位名稱與型態
                string declare = columnjson[i].Param;
                string name = declare.Substring(1);

                string type = columnjson[i].Type;

                string mode = columnjson[i].Mode;
                if (mode == "IN")
                {
                    switch (name)
                    {
                        case "mid":
                            txView.p.Add(declare, mid, DbType.Int32);
                            break;
                        case "sid":
                            txView.p.Add(declare, sid, DbType.Int32);
                            break;
                        default:
                            txView.p = addTypeQuery(httpContext, txView.p, declare, name, type);
                            break;
                    }

                    txView.sqlstring += @$"{(i == 0 ? " " : ", ")}{declare}";
                }

            }

            return txView;
        }
        public JsonObject getSPoutput(List<dynamic> outputs, DynamicParameters p)
        {
            JsonObject json = new JsonObject();
            foreach (var item in outputs)
            {
                string declare = item.Param;
                string type = item.Type;
                string name = declare.Substring(1);

                switch (type)
                {
                    case "int":
                        json.Add(name, p.Get<int>(declare));
                        break;
                    case "smallint":
                        json.Add(name, p.Get<int>(declare));
                        break;
                    case "tinyint":
                        json.Add(name, p.Get<int>(declare));
                        break;
                    case "float":
                        json.Add(name, p.Get<double>(declare));
                        break;
                    case "bit":
                        json.Add(name, p.Get<bool>(declare));
                        break;
                    case "uniqueidentifier":
                        json.Add(name, p.Get<Guid>(declare));
                        break;
                    default:
                        json.Add(name, p.Get<string>(declare));
                        break;
                }
            }

            return json;

        }

        public int? getMethodType(string method)
        {
            int? type;

            switch (method)
            {
                case "GET":
                    type = 0;
                    break;
                case "POST":
                    type = 1;
                    break;
                case "PUT":
                    type = 2;
                    break;
                case "DELETE":
                    type = 3;
                    break;
                default:
                    type = null;
                    break;
            }

            return type;
        }

        public void insertLogManTx(string method, string objectName, DynamicParameters sp_InOut, int? sid, AppDb db)
        {
            if (method != "GET" && sid != null)
            {
                string strsql = @"select parameter_id, parameter_name 
                            from vd_SystemObjectParametersSL 
                            where name = @objectName
                            order by parameter_id";

                var data = db.Connection.Query(strsql, new { objectName });

                if (data == null)
                    return;

                var list = new Dictionary<string, string>();

                foreach (var item in data)
                {
                    string value = Convert.ToString(sp_InOut.Get<dynamic>($"{item.parameter_name.Substring(1)}"));
                    list.Add(item.parameter_name, value);
                }

                string jsonInOut = JsonConvert.SerializeObject(list);

                string strSql = "[xp_insertLogManTxFromApi]";
                var p = new DynamicParameters();
                p.Add("@json", jsonInOut);
                p.Add("@sid", sid);
                p.Add("@name", objectName);
                p.Add("@method", method);
                db.Connection.Execute(strSql, p, commandType: CommandType.StoredProcedure);

            }
        }
    }
}
