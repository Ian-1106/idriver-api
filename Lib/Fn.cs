//using Newtonsoft.Json;
using Azure;
using Dapper;
using I3S_API.Model;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using I3S_API.Model;

namespace I3S_API.Lib
{
    public class Fn
    {
        public string Base64Encode(string AStr)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(AStr));
        }

        public string Base64Decrypt(string str)
        {
            try
            {
                string tmp = Encoding.UTF8.GetString(Convert.FromBase64String(str));
                Guid tmp2 = Guid.Parse(tmp);

                return tmp;
            }
            catch
            {
                return null;
            }
        }

        public T Post<T>(string apicmd, string ABase64, dynamic data, string baseaddress, out bool result)
        {
            result = false;
            HttpClient client = new HttpClient()
            {
                BaseAddress = new Uri(baseaddress)
            };

            if (!ABase64.IsNullOrEmpty())
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {ABase64}");

            string data_str = JsonConvert.SerializeObject(data);

            HttpContent content = new StringContent(data_str, Encoding.UTF8, "application/json");

            // 最終以post形式，發送request至驗證伺服器{apiURI}位置
            HttpResponseMessage response = client.PostAsync(apicmd, content).GetAwaiter().GetResult();
            if (response != null)
            {
                // 檢查response是否為200
                if (response.IsSuccessStatusCode)
                {
                    // 取得response json
                    string result_str = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    result = true;
                    return JsonConvert.DeserializeObject<T>(result_str, new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });
                }
                else
                {
                    return default;
                }
            }
            else
            {
                return default;
            }
        }

        static bool HasProperty(dynamic obj, string propertyName)
        {
            try
            {
                var dictionary = (IDictionary<string, object>)obj;
                return dictionary.ContainsKey(propertyName);
            }
            catch
            {
                return false;
            }
        }

        public T Get<T>(string url, dynamic data, out bool result)
        {
            result = false;
            HttpClient client = new HttpClient();

            if (HasProperty(data, "bearerToken"))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer  {data.bearerToken}");
            }

            HttpResponseMessage response = client.GetAsync(url).GetAwaiter().GetResult();
            if (response != null)
            {
                // 檢查response是否為200
                if (response.IsSuccessStatusCode)
                {
                    // 取得response json
                    string result_str = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    result = true;
                    return JsonConvert.DeserializeObject<T>(result_str, new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });
                }
                else
                {
                    return default;
                }
            }
            else
            {
                return default;
            }
        }

		#region 隨機生成指定長度的驗證碼字符串
		public string RandomCode(int length, string s = "0123456789")
		{
			StringBuilder sb = new StringBuilder();
			Random rand = new Random();
			int index;
			// 循環指定次數生成指定長度的隨機驗證碼字符串
			for (int i = 0; i < length; i++)
			{
				// 在指定的字串中隨機選取一個字符拼接到驗證碼字符串中
				index = rand.Next(0, s.Length);
				sb.Append(s[index]);
			}
			return sb.ToString();
		}
		#endregion

		#region 產生圖型干擾區域
		// 生成隨機背景顏色
		public void PaintRandomBackColor(Graphics g)
		{
			Random r = new Random();
			// 生成隨機顏色並設置為圖形的背景色
			Color color = Color.FromArgb(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256));
			g.Clear(color);
		}

		// 繪製背景干擾線
		public void PaintBackInterLine(Graphics g, int num, int width, int height)
		{
			Random r = new Random();
			int startX, startY, endX, endY;
			// 畫指定數量的干擾線
			for (int i = 0; i < num; i++)
			{
				startX = r.Next(0, width);
				startY = r.Next(0, height);
				endX = r.Next(0, width);
				endY = r.Next(0, height);
				// 在圖形上繪製干擾線
				g.DrawLine(new Pen(Brushes.Silver), startX, startY, endX, endY);
			}
			// 畫另一顏色的干擾線
			for (int i = 0; i < num; i++)
			{
				startX = r.Next(0, width);
				startY = r.Next(0, height);
				endX = r.Next(0, width);
				endY = r.Next(0, height);
				// 在圖形上繪製另一顏色的干擾線
				g.DrawLine(new Pen(Brushes.Purple), startX, startY, endX, endY);
			}
		}

		// 在圖形上繪製前景干擾點
		public void PaintFrontPoint(Bitmap map, int num, int width, int height)
		{
			Random r = new Random();
			int x, y;
			// 在圖形上隨機繪製指定數量的干擾點
			for (int i = 0; i < num; i++)
			{
				x = r.Next(width);
				y = r.Next(height);
				// 設置指定位置的顏色為隨機顏色，形成干擾點
				map.SetPixel(x, y, Color.FromArgb(r.Next()));
			}
		}
		#endregion

		// 使用 MD5 算法計算字符串的摘要值
		public string MD5Hash(string str)
		{
			using (var cryptoMD5 = MD5.Create())
			{
				// 將字串編碼成 UTF8 位元組陣列
				var bytes = Encoding.UTF8.GetBytes(str);
				// 計算 MD5 的雜湊值
				var hash = cryptoMD5.ComputeHash(bytes);
				// 將 byte 陣列轉換成 MD5 字符串表示形式
				var md5 = BitConverter.ToString(hash)
					.Replace("-", String.Empty)
					.ToUpper();
				return md5;
			}
		}

        public Uri? signInSSO(SignInSSO signInSSO, string Weburi, string state, int sid)
        {
            using (var db = new AppDb())
            {
                string strSql = "xp_signIn_sso";
                var p = new DynamicParameters();
                p.Add("@uid", signInSSO.uid);
                p.Add("@account", signInSSO.account);
                p.Add("@name", signInSSO.name);
                p.Add("@email", signInSSO.email);
                p.Add("@site", signInSSO.site);
                p.Add("@pic", signInSSO.picture);
                p.Add("@sid", signInSSO.sid);
                p.Add("@status", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                p.Add("@message", dbType: DbType.String, direction: ParameterDirection.Output, size: 255);
                db.Connection.Execute(strSql, p, commandType: CommandType.StoredProcedure);
                bool status = p.Get<bool>("@status");
                string message = p.Get<string>("@message");

                new Log().insertLogManTx("GET", strSql, p, sid, db);
            }

            Uri uri = new Uri($"{Weburi}{(state == null ? "" : state)}");
            return uri;
        }
    }
}
