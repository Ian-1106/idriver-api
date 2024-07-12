using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Microsoft.AspNetCore.Mvc;
using I3S_API.Lib;

// 命名空間定義了 Controller 的位置和使用的其他類型

namespace I3S_API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
	// 標記這是一個 Web API Controller，設置路由模式和 ResponseCache
	public class ImageValidateCodeController : ControllerBase
	{
		/// <summary>
		/// 取得驗證碼,並寫入cookie
		/// </summary>
		[HttpGet]
		public IActionResult Get([FromQuery] string CaptchaType)
		{
			// 讀取 JSON 檔案並將其內容讀入字串變數中
			string Captcha_Key = AppConfig.Config[$"Captcha_Key:{CaptchaType}"];

			// 從設定中取得 Cookie 過期時間並將其轉換為整數
			string CookieSetting_TimeStr = AppConfig.Config[$"CookieSetting:Time"];
			int CookieSetting_Time = int.Parse(CookieSetting_TimeStr);

			// 從設定中取得 Cookie 屬性 HttpOnly 的字串值並將其轉換為布林值
			string CookieSetting_HttpOnlyStr = AppConfig.Config[$"CookieSetting:HttpOnly"];
			bool CookieSetting_HttpOnly = bool.Parse(CookieSetting_HttpOnlyStr);

			// 從設定中取得 Cookie 屬性 Secure 的字串值並將其轉換為布林值
			string CookieSetting_SecureStr = AppConfig.Config[$"CookieSetting:Secure"];
			bool CookieSetting_Secure = bool.Parse(CookieSetting_SecureStr);

			// 從設定中取得 Cookie 屬性 SameSite 的字串值並將其轉換為列舉型別 SameSiteMode
			string CookieSetting_SameSiteStr = AppConfig.Config[$"CookieSetting:SameSite"];
			SameSiteMode CookieSetting_SameSite = (SameSiteMode)Enum.Parse(typeof(SameSiteMode), CookieSetting_SameSiteStr);


			// 設置圖片驗證碼的相關屬性
			int cvWidth = 180;
			int cvHeight = 60;
			float fontSize = 28f;
			int interLine = 25;
			int point = 100;

			// 創建 Fn 實例
			Fn Fn = new Fn();
			// 生成隨機驗證碼
			string code = Fn.RandomCode(6);

			// 創建一個內存流用於保存圖片
			MemoryStream ms = new MemoryStream();
			using (Bitmap map = new Bitmap(cvWidth, cvHeight))
			{
				// 在圖片上繪製各種元素
				using (Graphics g = Graphics.FromImage(map))
				{
					// 繪製背景顏色
					Fn.PaintRandomBackColor(g);
					// 繪製干擾線
					Fn.PaintBackInterLine(g, interLine, map.Width, map.Height);
					// 繪製文字驗證碼
					Font font = new Font("JhengHei", fontSize, (FontStyle.Bold | FontStyle.Italic));
					LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, 0, map.Width, map.Height), Color.Blue, Color.DarkRed, 1.2f, true);
					g.DrawString(code, font, brush, new Point(10, 8));
					// 繪製干擾點
					Fn.PaintFrontPoint(map, point, map.Width, map.Height);
					// 繪製邊框線
					g.DrawRectangle(new Pen(Color.Silver), 0, 0, map.Width - 1, map.Height - 1);
				}
				// 將圖片保存到內存流中
				map.Save(ms, ImageFormat.Jpeg);
			}
			byte[] data = ms.GetBuffer();

			string Captcha_EncryptString = Fn.MD5Hash(code + Captcha_Key);

			// 將生成的驗證碼存儲到 Cookie 中
			Response.Cookies.Append(CaptchaType, Captcha_EncryptString, new CookieOptions
			{
				// 在這裡設置 Cookie 的相關選項，例如過期時間、域名、路徑等
				Expires = DateTime.UtcNow.AddMinutes(CookieSetting_Time), // 設置 Cookie 的過期時間為 10 分鐘後
				HttpOnly = CookieSetting_HttpOnly, // 設置為 HttpOnly，防止客戶端 JavaScript 訪問
				Secure = CookieSetting_Secure, // 如果您的網站使用了 SSL/TLS，可以將 Secure 設置為 true，增加安全性
				SameSite = CookieSetting_SameSite // 設置 SameSite，以防止跨站點請求偽造攻擊
			});

			// 返回圖片文件給客戶端
			return File(data, "image/jpeg");
		}
	}
}
