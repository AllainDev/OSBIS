namespace ORBIS.Services
{
    public interface IRegistrationService
    {
        Task<ServiceResult> StartRegistrationAsync(
        string fullName,
        string email,
        string password,
        CancellationToken cancellationToken = default);

        Task<ServiceResult> VerifyOtpAsync(
            string email,
            string otp,
            CancellationToken cancellationToken = default);

        Task<ServiceResult> ResendOtpAsync(
            string email,
            CancellationToken cancellationToken = default);
    }
}
