using Microsoft.AspNetCore.Http;
using OSBIS.Common;
using System.Threading.Tasks;

namespace OSBIS.Middleware
{
    /// <summary>
    /// Security Headers Middleware (OWASP best practices)
    /// - CSP: Ngăn XSS
    /// - X-Frame-Options: Ngăn clickjacking
    /// - X-Content-Type-Options: Ngăn MIME sniffing
    /// - Referrer-Policy: Bảo vệ referrer
    /// - Permissions-Policy: Tắt các tính năng không cần
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Xử lý response headers TRƯỚC khi gọi next
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;

                // Content Security Policy
                if (!headers.ContainsKey("Content-Security-Policy"))
                    headers["Content-Security-Policy"] = AppConstants.ContentSecurityPolicy;

                // Prevent clickjacking
                if (!headers.ContainsKey("X-Frame-Options"))
                    headers["X-Frame-Options"] = "DENY";

                // Prevent MIME sniffing
                if (!headers.ContainsKey("X-Content-Type-Options"))
                    headers["X-Content-Type-Options"] = "nosniff";

                // XSS Protection (legacy)
                if (!headers.ContainsKey("X-XSS-Protection"))
                    headers["X-XSS-Protection"] = "1; mode=block";

                // Referrer Policy
                if (!headers.ContainsKey("Referrer-Policy"))
                    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

                // Permissions Policy
                if (!headers.ContainsKey("Permissions-Policy"))
                    headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

                // HSTS - chỉ trong production
                if (!context.Request.IsHttps == false && !headers.ContainsKey("Strict-Transport-Security"))
                {
                    headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
                }

                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}