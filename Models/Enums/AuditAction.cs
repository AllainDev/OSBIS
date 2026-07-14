namespace OSBIS.Models.Enums
{
    /// <summary>
    /// Hành động được ghi log audit
    /// </summary>
    public enum AuditAction : byte
    {
        Login = 0,
        LoginFailed = 1,
        Logout = 2,
        Lockout = 3,
        Register = 4,
        PasswordChange = 5,
        PasswordReset = 6,
        UserCreate = 10,
        UserUpdate = 11,
        UserDelete = 12,
        UserLock = 13,
        UserUnlock = 14,
        RoleAssign = 20,
        AccessDenied = 30,
        SensitiveDataAccess = 31,
        // Phase 3 - Order/Voucher
        OrderPlaced = 40,
        OrderCancelled = 41,
        OrderConfirmed = 42,
        VoucherUsed = 50,
        VoucherCreated = 51,
        // Phase 4 - Payment/Shipment/Review
        PaymentConfirmed = 60,
        PaymentRefunded = 61,
        ShipmentCreated = 70,
        ShipmentUpdated = 71,
        ReviewCreated = 72,
        // Phase 5
        NotificationSent = 80,
        SystemConfigChanged = 90,
        NotificationSentToUser = 91
    }
}
