using System.Security.Claims;
using OSBIS.Models.Entities;

namespace OSBIS.Common
{
    public static class ClaimsPrincipalExtensions
    {
        public static int? GetUserId(this ClaimsPrincipal principal)
        {
            var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out int id) ? id : null;
        }

        public static string? GetUsername(this ClaimsPrincipal principal)
        {
            return principal.FindFirstValue(ClaimTypes.Name);
        }

        public static string? GetRole(this ClaimsPrincipal principal)
        {
            return principal.FindFirstValue(ClaimTypes.Role);
        }

        public static string? GetFullName(this ClaimsPrincipal principal)
        {
            return principal.FindFirstValue("FullName");
        }

        public static string? GetEmail(this ClaimsPrincipal principal)
        {
            return principal.FindFirstValue(ClaimTypes.Email);
        }

        public static bool IsAdmin(this ClaimsPrincipal principal) =>
            principal.IsInRole(AppConstants.Roles.Admin);

        public static bool IsStaff(this ClaimsPrincipal principal) =>
            principal.IsInRole(AppConstants.Roles.Staff) || principal.IsInRole(AppConstants.Roles.Admin);

        public static bool IsAuthenticated(this ClaimsPrincipal principal) =>
            principal.Identity?.IsAuthenticated == true;
    }
}