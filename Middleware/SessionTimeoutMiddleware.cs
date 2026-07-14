using Microsoft.AspNetCore.Http;
using OSBIS.Common;
using System.Threading.Tasks;

namespace OSBIS.Middleware
{
    /// <summary>
    /// Session Timeout Middleware (BR07)
    /// Kiểm tra thời gian không hoạt động, tự động đăng xuất nếu quá 30 phút
    /// </summary>
    public class SessionTimeoutMiddleware
    {
        private readonly RequestDelegate _next;
        private const string LastActivityKey = "LastActivity";

        public SessionTimeoutMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Chỉ áp dụng cho user đã đăng nhập
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var now = DateTime.UtcNow;

                if (context.Session.IsAvailable)
                {
                    var lastActivity = context.Session.GetString(LastActivityKey);
                    if (!string.IsNullOrEmpty(lastActivity) &&
                        DateTime.TryParse(lastActivity, out DateTime last))
                    {
                        var elapsed = now - last;
                        if (elapsed.TotalMinutes > AppConstants.SessionTimeoutMinutes)
                        {
                            // Hết hạn session
                            context.Session.Clear();
                            // Redirect về login nếu là request MVC
                            if (!context.Request.Path.StartsWithSegments("/api") &&
                                context.Request.Method == "GET")
                            {
                                context.Response.Redirect("/Account/Login?timeout=1");
                                return;
                            }
                        }
                    }
                    context.Session.SetString(LastActivityKey, now.ToString("O"));
                }
            }

            await _next(context);
        }
    }
}