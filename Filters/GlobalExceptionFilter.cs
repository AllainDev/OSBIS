using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Serilog;

namespace OSBIS.Filters
{
    /// <summary>
    /// Global Exception Filter - Bắt tất cả exception chưa được xử lý
    /// Log + Trả về trang Error thân thiện
    /// </summary>
    public class GlobalExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            // Log chi tiết
            Log.Error(context.Exception,
                "Unhandled exception at {Path} | User: {User}",
                context.HttpContext.Request.Path,
                context.HttpContext.User?.Identity?.Name ?? "Anonymous");

            // 404 vs 500
            if (context.Exception is NotImplementedException)
            {
                context.Result = new ViewResult
                {
                    ViewName = "Error",
                    StatusCode = 501
                };
            }
            else
            {
                context.Result = new ViewResult
                {
                    ViewName = "Error",
                    StatusCode = 500
                };
            }

            context.ExceptionHandled = true;
        }
    }
}