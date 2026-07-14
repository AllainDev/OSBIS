using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OSBIS.Common;
using OSBIS.Models.Enums;
using OSBIS.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OSBIS.Middleware
{
    /// <summary>
    /// Audit Log Middleware - Ghi log cho các action quan trọng (BR09)
    /// </summary>
    public class AuditLogMiddleware
    {
        private readonly RequestDelegate _next;

        // Các endpoint cần log
        private static readonly string[] TrackedPaths = new[]
        {
            "/Account/Logout",
            "/Account/ChangePassword",
            "/Admin/Users",
            "/Admin/Users/Create",
            "/Admin/Users/Edit",
            "/Admin/Users/Delete",
            "/Admin/Users/Lock",
            "/Admin/Users/Unlock"
        };

        public AuditLogMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            // Log sau khi response hoàn thành
            if (ShouldLog(context))
            {
                try
                {
                    using var scope = context.RequestServices.CreateScope();
                    var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();

                    var userId = context.User.GetUserId();
                    var username = context.User.GetUsername() ?? "Anonymous";
                    var ip = context.GetClientIpAddress();
                    var isSuccess = context.Response.StatusCode < 400;

                    var action = GetActionFromPath(context.Request.Path);
                    if (action.HasValue)
                    {
                        await auditLogService.LogAsync(
                            userId, username, action.Value,
                            description: $"{context.Request.Method} {context.Request.Path}",
                            isSuccess: isSuccess,
                            controller: GetControllerFromPath(context.Request.Path),
                            actionName: GetActionNameFromPath(context.Request.Path));
                    }
                }
                catch
                {
                    // Không throw - không làm crash request
                }
            }
        }

        private static bool ShouldLog(HttpContext context)
        {
            if (context.Request.Method == "GET") return false;

            var path = context.Request.Path.Value ?? "";
            return TrackedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        private static AuditAction? GetActionFromPath(PathString path)
        {
            var p = path.Value?.ToLower() ?? "";
            if (p.Contains("/logout")) return AuditAction.Logout;
            if (p.Contains("/changepassword")) return AuditAction.PasswordChange;
            if (p.Contains("/users/create")) return AuditAction.UserCreate;
            if (p.Contains("/users/edit")) return AuditAction.UserUpdate;
            if (p.Contains("/users/delete")) return AuditAction.UserDelete;
            if (p.Contains("/users/lock")) return AuditAction.UserLock;
            if (p.Contains("/users/unlock")) return AuditAction.UserUnlock;
            return null;
        }

        private static string? GetControllerFromPath(PathString path)
        {
            var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments?.Length > 1 ? segments[1] : null;
        }

        private static string? GetActionNameFromPath(PathString path)
        {
            var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments?.Length > 2 ? segments[2] : null;
        }
    }
}