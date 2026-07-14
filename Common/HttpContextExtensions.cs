using Microsoft.AspNetCore.Http;

namespace OSBIS.Common
{
    public static class HttpContextExtensions
    {
        public static string GetClientIpAddress(this HttpContext context)
        {
            // Kiểm tra X-Forwarded-For (proxy/load balancer)
            string? ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(ip))
            {
                // Lấy IP đầu tiên (client gốc)
                ip = ip.Split(',')[0].Trim();
            }

            if (string.IsNullOrEmpty(ip))
            {
                ip = context.Connection.RemoteIpAddress?.ToString();
            }

            return ip ?? "Unknown";
        }

        public static string GetUserAgent(this HttpContext context)
        {
            return context.Request.Headers["User-Agent"].ToString() ?? "Unknown";
        }
    }
}