namespace ORBIS.Services
{
    public class OtpSettings
    {
        public string Secret { get; set; } = string.Empty;

        public int LifetimeMinutes { get; set; } = 5;

        public int ResendCooldownSeconds { get; set; } = 60;

        public int MaximumFailedAttempts { get; set; } = 5;
    }
}
