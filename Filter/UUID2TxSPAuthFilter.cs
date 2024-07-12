using I3S_API.Lib;
using I3S_API.Model;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Dapper;
using System.Security.Cryptography;
using System.Security;
using System.Runtime.InteropServices;

namespace I3S_API.Filter
{
    public class UUID2TxSPAuthFilter : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            HttpContext httpContext = context.HttpContext;

            int mid = (int)httpContext.Items["MID"];
            if (mid == null || mid <= 0)
            {
                context.Result = new myUnauthorizedResult("無權限.");
                return;
            }

            UUID fn_uuid = new UUID();
            Param fn_param = new Param();

            dynamic? bodyParam = fn_param.getBodyParamByJson(httpContext);
            if (bodyParam == null)
            {
                context.Result = new myUnauthorizedResult("錯誤傳遞", 400);
                return;
            }

            Guid? uuid = fn_param.getValueFromRoute<Guid?>(httpContext, "uuid");

            if (uuid == null)
            {
                context.Result = new myUnauthorizedResult("無權限..");
                return;
            }

            int? methodType = fn_uuid.getMethodType(httpContext.Request.Method.ToString());

            if (methodType == null)
            {
                context.Result = new myUnauthorizedResult("無權限...");
                return;
            }

            string strsql = @$"select Name, PermissionType, Param, Name2 
                                from vd_UUID2TxSP
                                where UUID = @uuid and MethodType = @methodType";

            using (var db = new AppDb())
            {
                dynamic data = db.Connection.QueryFirstOrDefault(strsql, new { uuid, methodType });

                if (data == null || data?.Name == null)
                {
                    context.Result = new myUnauthorizedResult("無效");
                    return;
                }

                //會員模式
                bool midmode = data.PermissionType == null ? true : false;

                int? cid = fn_param.getParamFromBody<int?>("cid", bodyParam);

                //必須傳CID，會員模式可不傳CID
                if (cid == null && !midmode)
                {
                    context.Result = new myUnauthorizedResult("無權限");
                    return;
                }

                if (!midmode)
                {
                    myUnauthorizedResult myUnauthorizedResult = fn_uuid.checkCIDPermission(data.PermissionType, mid, cid);
                    if (myUnauthorizedResult != null)
                    {
                        context.Result = myUnauthorizedResult;
                        return;
                    }
                }

                httpContext.Items.Add("UUID_data", data);
                httpContext.Items.Add("CID", cid);
                httpContext.Items.Add("midmode", midmode);
                httpContext.Items.Add("bodyParam", bodyParam);
                httpContext.Items.Add("ObjectName", data.Name2);
            }


        }
    }
}
