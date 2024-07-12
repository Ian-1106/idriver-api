using I3S_API.Lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Dapper;
using System.Text;
using I3S_API.Model;

namespace I3S_API.Filter
{
    public class AuthFilter : Attribute, IAuthorizationFilter
    {
        private readonly string _permission;
        private readonly string _checkparam;
        public AuthFilter(string permission, string checkparam = "cid")
        {
            _permission = permission;
            _checkparam = checkparam;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            HttpContext httpContext = context.HttpContext;
            Param fn_param = new Param();

            int? cid = fn_param.getCIDFromAll(httpContext, _checkparam);

            if (cid == null)
            {
                context.Result = new myUnauthorizedResult("無權限");
            }

            int mid = (int)httpContext.Items["MID"];


            string checkPermissionSQL = @$"select * from fn_getCIDPermission(@cid, @mid)";

            PermissionModel permissionModel = new PermissionModel();
            using (var db = new AppDb())
            {
                //取得會員在cid的所有權限
                permissionModel = db.Connection.QueryFirstOrDefault<PermissionModel>(checkPermissionSQL, new { cid, mid });

                //透過參數名稱(變數)，從model中找值
                bool allow = (bool)permissionModel.GetType().GetProperty(_permission).GetValue(permissionModel, null);

                if (!allow)
                {
                    context.Result = new myUnauthorizedResult("無權限");
                }
            }

            httpContext.Items.Add("PermissionModel", permissionModel);
        }
        
    }
}
