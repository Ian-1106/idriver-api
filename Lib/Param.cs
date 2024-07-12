using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.ComponentModel;

namespace I3S_API.Lib
{
    public class Param
    {
        public T? getParamFromQuery<T>(HttpContext httpContext, string keyname)
        {
            var param = httpContext.Request.Query;
            string? t = null;
            if (param.ContainsKey(keyname))
            {
                t = param[keyname].ToString();
            }

            return t.IsNullOrEmpty() ? default(T?) : ConvertType<T?>(t);
        }

        public T? getValueFromRoute<T>(HttpContext httpContext, string keyname)
        {
            var param = httpContext.Request.RouteValues;
            string? t = null;
            if (param.ContainsKey(keyname))
            {
                t = param[keyname].ToString();
            }

            return t.IsNullOrEmpty() ? default(T?) : ConvertType<T?>(t);
        }
        public T? ConvertType<T>(string value)
        {
            try
            {
                TypeConverter typeConverter = TypeDescriptor.GetConverter(typeof(T?));
                object propValue = typeConverter.ConvertFromString(value);

                return ChangeType<T?>(propValue);
            }
            catch
            {
                return default(T?);
            }
        }

        public static T? ChangeType<T>(object value)
        {
            var t = typeof(T?);

            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                {
                    return default(T?);
                }

                t = Nullable.GetUnderlyingType(t);
            }

            try
            {
                return (T?)Convert.ChangeType(value, t);
            }
            catch
            {
                return default(T?);
            }

        }

        public T? getParamFromBody<T>(string checkparam, dynamic paramBody)
        {
            if (paramBody.ContainsKey(checkparam))
            {
                var t = paramBody[checkparam];
                if (t == null)
                    return default(T?);
                else
                    return ConvertType<T?>(t.ToString());
            }
            else
            {
                return default(T?);
            }
        }

        public int? getCIDFromAll(HttpContext httpContext, string checkparam)
        {
            int? cid = null;

            cid = getValueFromRoute<int?>(httpContext, checkparam);

            if (cid == null)
            {
                if (httpContext.Request.Method == "GET")
                {
                    cid = getParamFromQuery<int?>(httpContext, checkparam);
                }
                else if (httpContext.Request.ContentType == "application/json")
                {
                    httpContext.Request.EnableBuffering();

                    string RequestBody = new StreamReader(httpContext.Request.BodyReader.AsStream()).ReadToEnd();
                    httpContext.Request.Body.Position = 0;

                    dynamic columnjson = JsonConvert.DeserializeObject(RequestBody);
                    try
                    {
                        cid = (int)columnjson[checkparam];
                    }
                    catch
                    {
                        cid = null;
                    }
                }
                else
                {
                    try
                    {
                        cid = Int32.Parse(httpContext.Request.Form[checkparam][0]);
                    }
                    catch
                    {
                        cid = null;
                    }
                }
            }
            return cid;
        }

        public dynamic? getBodyParamByJson(HttpContext httpContext)
        {
            if (httpContext.Request.ContentType == "application/json")
            {
                httpContext.Request.EnableBuffering();

                string RequestBody = new StreamReader(httpContext.Request.BodyReader.AsStream()).ReadToEnd();
                httpContext.Request.Body.Position = 0;

                dynamic? columnjson = JsonConvert.DeserializeObject(RequestBody);
                return columnjson;
            }

            return null;
        }
    }
}
