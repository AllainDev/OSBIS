namespace ORBIS.Services
{
    public interface IEmailService
    {
        Task SendRegistrationOtpAsync(
    string receiverEmail,
    string otp,
    int lifetimeMinutes,
    CancellationToken cancellationToken = default);
    }
}
