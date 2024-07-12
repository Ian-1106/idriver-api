using Dapper;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Data;

namespace I3S_API.Lib
{
    public class Log
    {
        public string Error2txtString(HttpContext context, string errMessage)
        {
            string path = context.Request.Path.ToString();
            string method = context.Request.Method.ToString();
            string SID = context.Items["SID"]?.ToString() ?? "-1";

            int bErrorLog = (int)context.Items["bErrorLog"];
            string objectName = context.Items["ObjectName"]?.ToString();

            string parameters = "Parameters :[";

            bool bParam = false;
            IDictionary<string, object> param_ds = null;

            if (bErrorLog == 0)
            {
                return $"\nSID:{SID}\n{method} - {path}\n{errMessage}\n";
            }
            else if (!objectName.IsNullOrEmpty())
            {
                try
                {
                    string strsql = @$"exec xp_getParamDS @objectName";

                    using (var db = new AppDb())
                    {
                        var data = db.Connection.QueryFirstOrDefault(strsql, new { objectName });

                        param_ds = (IDictionary<string, object>)data;
                    }
                }
                catch (Exception ex)
                {
                    param_ds = null;
                }

            }

            //從GET，Body，Form取得參數
            if (method == "GET")
            {
                var query = context.Request.Query;

                if (query.Count > 0)
                {
                    bParam = true;
                    parameters = "Query - " + parameters;

                    foreach (var item in query)
                    {
                        if (writeParam("_" + item.Key, param_ds))
                            parameters += $"{item.Key}:{item.Value}, ";
                    }
                }
            }
            else if (context.Request.ContentType == "application/json")
            {
                if (context.Request.ContentLength > 0)
                {
                    bParam = true;
                    parameters = "Body - " + parameters;

                    context.Request.Body.Position = 0;

                    try
                    {
                        string RequestBody = new StreamReader(context.Request.Body).ReadToEnd();

                        var columnjson = (JObject)JsonConvert.DeserializeObject(RequestBody);

                        foreach (var item in columnjson)
                        {
                            if (writeParam("_@" + item.Key, param_ds))
                                parameters += $"{item.Key}:{item.Value}, ";
                        }
                    }
                    catch (Exception ex)
                    {
                        bParam = false;
                    }

                }
            }
            else
            {
                bParam = true;
                try
                {
                    var form = context.Request.Form;
                    if (form.Count > 0)
                    {
                        parameters = "Form - " + parameters;
                        foreach (var item in form)
                        {
                            if (writeParam("_@" + item.Key, param_ds))
                                parameters += $"{item.Key}:{item.Value}, ";
                        }
                    }
                }
                catch
                {
                    //不是query、body、form
                    parameters = "[未知傳遞方式";
                }
            }

            parameters += "]";

            return $"\nSID:{SID}\n{method} - {path}\n{(bParam ? parameters + "\n" : "")}{errMessage}\n";
        }

        public bool writeParam(string key, IDictionary<string, object> param_ds)
        {
            if (param_ds != null)
            {
                var ds = param_ds[key]?.ToString();

                if (ds == null || ds == "" || ds == "0")
                {
                    return true;
                }
            }
            else
            {
                return true;
            }

            return false;
        }

        public void insertLogManTx(string method, string objectName, DynamicParameters sp_InOut, int? sid, AppDb db)
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
