namespace OSBIS.Common
{
    /// <summary>
    /// Application-wide constants
    /// </summary>
    public static class AppConstants
    {
        // Cookie / Session
        public const string AuthCookieName = "OSBIS.Auth";
        public const int SessionTimeoutMinutes = 30;

        // Account Lockout (BR06)
        public static class Lockout
        {
            public const int MaxFailedAttempts = 5;
            public const int LockoutMinutes = 15;
        }

        // Roles
        public static class Roles
        {
            public const string Admin = "Admin";
            public const string Staff = "Staff";
            public const string Customer = "Customer";
            public const string Delivery = "Delivery";
        }

        // Policies
        public static class Policies
        {
            public const string RequireAdmin = "RequireAdmin";
            public const string RequireStaff = "RequireStaff";
            public const string RequireCustomer = "RequireCustomer";
            public const string RequireDelivery = "RequireDelivery";
        }

        // Default password khi admin tạo user
        public const string DefaultPassword = "User@123";

        // Security headers
        public const string ContentSecurityPolicy =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
            "font-src 'self' https://fonts.gstatic.com data: https://cdn.jsdelivr.net; " +
            "img-src 'self' data: https: blob:; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'";
    }
}