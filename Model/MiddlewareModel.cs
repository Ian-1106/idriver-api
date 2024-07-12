namespace I3S_API.Model
{
    public class cookieModel
    {
        public CookieOptions Options { get; set; }

        public CookieOptions Options_Refresh { get; set; }

        public cookieModel(bool ClearWhenClose, DateTime ExpiredDT, DateTime ExpiredDT_Refresh, bool IsDevelopment)
        {
            Options = new CookieOptions
            {
                Secure = true,
                HttpOnly = true,
                SameSite = IsDevelopment ? SameSiteMode.None : SameSiteMode.Lax,
                Expires = ClearWhenClose == true ? null : ExpiredDT
            };

            Options_Refresh = new CookieOptions
            {
                Secure = true,
                HttpOnly = true,
                SameSite = IsDevelopment ? SameSiteMode.None : SameSiteMode.Lax,
                Expires = ClearWhenClose == true ? null : ExpiredDT_Refresh
            };
        }
    }
}
