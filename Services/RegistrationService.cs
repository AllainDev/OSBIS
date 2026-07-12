
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ORBIS.Data;
using ORBIS.Models.Entities;
using System.Security.Cryptography;
using System.Text;

namespace ORBIS.Services
{
    public class RegistrationService : IRegistrationService
    {
        private readonly AppDbContext _dbContext;
        private readonly IEmailService _emailService;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly OtpSettings _otpSettings;
        private readonly RegistrationSettings _registrationSettings;
        private readonly ILogger<RegistrationService> _logger;
        public RegistrationService(
       AppDbContext dbContext,
       IEmailService emailService,
       IPasswordHasher<User> passwordHasher,
       IOptions<OtpSettings> otpOptions,
       IOptions<RegistrationSettings> registrationOptions,
       ILogger<RegistrationService> logger)
        {
            _dbContext = dbContext;
            _emailService = emailService;
            _passwordHasher = passwordHasher;
            _otpSettings = otpOptions.Value;
            _registrationSettings = registrationOptions.Value;
            _logger = logger;

            ValidateSettings();
        }
        public async Task<ServiceResult> ResendOtpAsync(string email, CancellationToken cancellationToken = default)
        {
            string cleanedEmail = NormalizeEmail(email);
            DateTime nowUtc = DateTime.UtcNow;

            PendingRegistration? pending =
                await _dbContext.PendingRegistrations
                    .SingleOrDefaultAsync(
                        item => item.Email == cleanedEmail,
                        cancellationToken);

            if (pending is null)
            {
                return ServiceResult.Failure(
                    "Không tìm thấy yêu cầu đăng ký.");
            }

            double elapsedSeconds =
                (nowUtc - pending.LastOtpSentAtUtc)
                .TotalSeconds;

            if (elapsedSeconds <
                _otpSettings.ResendCooldownSeconds)
            {
                int remainingSeconds =
                    _otpSettings.ResendCooldownSeconds -
                    Math.Max(0, (int)elapsedSeconds);

                return ServiceResult.Failure(
                    $"Vui lòng đợi {remainingSeconds} giây " +
                    "trước khi gửi lại OTP.");
            }

            string otp = GenerateOtp();

            pending.OtpHash = HashOtp(
                cleanedEmail,
                otp);

            pending.OtpExpiresAtUtc = nowUtc.AddMinutes(
                _otpSettings.LifetimeMinutes);

            pending.LastOtpSentAtUtc = nowUtc;
            pending.FailedAttempts = 0;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            try
            {
                await _emailService.SendRegistrationOtpAsync(
                    pending.Email,
                    otp,
                    _otpSettings.LifetimeMinutes,
                    cancellationToken);
            }
            catch
            {
                pending.OtpExpiresAtUtc = DateTime.UtcNow;

                pending.LastOtpSentAtUtc =
                    DateTime.UtcNow.AddSeconds(
                        -_otpSettings.ResendCooldownSeconds);

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                return ServiceResult.Failure(
                    "Không thể gửi lại OTP. " +
                    "Vui lòng thử lại sau.");
            }

            return ServiceResult.Success(
                "Mã OTP mới đã được gửi đến email của bạn.");
        }

        public async Task<ServiceResult> StartRegistrationAsync(string fullName, string email, string password, CancellationToken cancellationToken = default)
        {
            string cleanedFullName = fullName.Trim();
            string cleanedEmail = NormalizeEmail(email);
            DateTime nowUtc = DateTime.UtcNow;

            bool emailAlreadyExists = await _dbContext.Users
                .AsNoTracking()
                .AnyAsync(
                    user => user.Email.ToLower() == cleanedEmail,
                    cancellationToken);

            if (emailAlreadyExists)
            {
                return ServiceResult.Failure(
                    "Email này đã được đăng ký.");
            }

            PendingRegistration? pending =
                await _dbContext.PendingRegistrations
                    .SingleOrDefaultAsync(
                        item => item.Email == cleanedEmail,
                        cancellationToken);

            if (pending is not null)
            {
                double elapsedSeconds =
                    (nowUtc - pending.LastOtpSentAtUtc)
                    .TotalSeconds;

                if (elapsedSeconds <
                    _otpSettings.ResendCooldownSeconds)
                {
                    int remainingSeconds =
                        _otpSettings.ResendCooldownSeconds -
                        Math.Max(0, (int)elapsedSeconds);

                    return ServiceResult.Failure(
                        $"Vui lòng đợi {remainingSeconds} giây " +
                        "trước khi yêu cầu OTP mới.");
                }
            }

            string username;

            if (pending is null)
            {
                username = await GenerateUniqueUsernameAsync(
                    cleanedEmail,
                    cancellationToken);
            }
            else
            {
                username = pending.Username;
            }

            var userForHashing = new User
            {
                RoleId = _registrationSettings.DefaultRoleId,
                Username = username,
                PasswordHash = string.Empty,
                FullName = cleanedFullName,
                Email = cleanedEmail,
                IsActive = false,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc
            };

            string passwordHash =
                _passwordHasher.HashPassword(
                    userForHashing,
                    password);

            string otp = GenerateOtp();

            string otpHash = HashOtp(
                cleanedEmail,
                otp);

            if (pending is null)
            {
                pending = new PendingRegistration
                {
                    FullName = cleanedFullName,
                    Email = cleanedEmail,
                    Username = username,
                    PasswordHash = passwordHash,
                    RoleId = _registrationSettings.DefaultRoleId,
                    OtpHash = otpHash,
                    OtpExpiresAtUtc = nowUtc.AddMinutes(
                        _otpSettings.LifetimeMinutes),
                    LastOtpSentAtUtc = nowUtc,
                    FailedAttempts = 0,
                    CreatedAtUtc = nowUtc
                };

                _dbContext.PendingRegistrations.Add(pending);
            }
            else
            {
                pending.FullName = cleanedFullName;
                pending.PasswordHash = passwordHash;
                pending.OtpHash = otpHash;

                pending.OtpExpiresAtUtc = nowUtc.AddMinutes(
                    _otpSettings.LifetimeMinutes);

                pending.LastOtpSentAtUtc = nowUtc;
                pending.FailedAttempts = 0;
            }

            try
            {
                await _dbContext.SaveChangesAsync(
                    cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(
                    exception,
                    "Không lưu được đăng ký tạm cho {Email}",
                    cleanedEmail);

                return ServiceResult.Failure(
                    "Không thể tạo yêu cầu đăng ký. " +
                    "Vui lòng thử lại.");
            }

            try
            {
                await _emailService.SendRegistrationOtpAsync(
                    cleanedEmail,
                    otp,
                    _otpSettings.LifetimeMinutes,
                    cancellationToken);
            }
            catch
            {
                // Vô hiệu hóa OTP vừa tạo và cho phép gửi lại ngay.
                pending.OtpExpiresAtUtc = DateTime.UtcNow;

                pending.LastOtpSentAtUtc =
                    DateTime.UtcNow.AddSeconds(
                        -_otpSettings.ResendCooldownSeconds);

                try
                {
                    await _dbContext.SaveChangesAsync(
                        cancellationToken);
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Không cập nhật được trạng thái OTP lỗi.");
                }

                return ServiceResult.Failure(
                    "Không thể gửi OTP. " +
                    "Vui lòng kiểm tra email hoặc thử lại.");
            }

            return ServiceResult.Success(
                "Mã OTP đã được gửi đến email của bạn.");
        }

        public async Task<ServiceResult> VerifyOtpAsync(string email, string otp, CancellationToken cancellationToken = default)
        {
            string cleanedEmail = NormalizeEmail(email);
            string cleanedOtp = otp.Trim();
            DateTime nowUtc = DateTime.UtcNow;

            PendingRegistration? pending =
                await _dbContext.PendingRegistrations
                    .SingleOrDefaultAsync(
                        item => item.Email == cleanedEmail,
                        cancellationToken);

            if (pending is null)
            {
                return ServiceResult.Failure(
                    "Không tìm thấy yêu cầu đăng ký.");
            }

            if (pending.OtpExpiresAtUtc <= nowUtc)
            {
                return ServiceResult.Failure(
                    "Mã OTP đã hết hạn. Vui lòng gửi lại mã mới.");
            }

            if (pending.FailedAttempts >=
                _otpSettings.MaximumFailedAttempts)
            {
                return ServiceResult.Failure(
                    "Bạn đã nhập sai OTP quá nhiều lần. " +
                    "Vui lòng gửi lại OTP.");
            }

            bool otpIsValid = VerifyOtpHash(
                pending.OtpHash,
                cleanedEmail,
                cleanedOtp);

            if (!otpIsValid)
            {
                pending.FailedAttempts++;

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                int remainingAttempts =
                    _otpSettings.MaximumFailedAttempts -
                    pending.FailedAttempts;

                if (remainingAttempts <= 0)
                {
                    return ServiceResult.Failure(
                        "Bạn đã nhập sai OTP quá nhiều lần. " +
                        "Vui lòng gửi lại OTP mới.");
                }

                return ServiceResult.Failure(
                    $"Mã OTP không chính xác. " +
                    $"Bạn còn {remainingAttempts} lần thử.");
            }

            bool emailAlreadyExists = await _dbContext.Users
                .AnyAsync(
                    user => user.Email.ToLower() == cleanedEmail,
                    cancellationToken);

            if (emailAlreadyExists)
            {
                _dbContext.PendingRegistrations.Remove(pending);

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                return ServiceResult.Failure(
                    "Email này đã được đăng ký.");
            }

            // Kiểm tra lại username vì tài khoản khác có thể
            // đã lấy username trong thời gian chờ xác thực OTP.
            bool usernameAlreadyExists =
                await _dbContext.Users.AnyAsync(
                    user =>
                        user.Username.ToLower() ==
                        pending.Username.ToLower(),
                    cancellationToken);

            if (usernameAlreadyExists)
            {
                pending.Username =
                    await GenerateUniqueUsernameAsync(
                        pending.Email,
                        cancellationToken);
            }

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var newUser = new User
                {
                    RoleId = pending.RoleId,
                    Username = pending.Username,
                    PasswordHash = pending.PasswordHash,
                    FullName = pending.FullName,
                    Email = pending.Email,
                    Phone = null,
                    IsActive = true,
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc
                };

                _dbContext.Users.Add(newUser);

                _dbContext.PendingRegistrations.Remove(pending);

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                return ServiceResult.Success(
                    "Đăng ký tài khoản thành công.");
            }
            catch (DbUpdateException exception)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                _logger.LogError(
                    exception,
                    "Không tạo được tài khoản {Email}",
                    cleanedEmail);

                return ServiceResult.Failure(
                    "Không thể tạo tài khoản. " +
                    "Email, username hoặc RoleId có thể không hợp lệ.");
            }
        }
        private async Task<string> GenerateUniqueUsernameAsync(
       string email,
       CancellationToken cancellationToken)
        {
            string emailPrefix = email.Split('@')[0];

            string baseUsername = new string(
                emailPrefix
                    .Where(character =>
                        char.IsLetterOrDigit(character) ||
                        character == '_')
                    .Select(char.ToLowerInvariant)
                    .ToArray());

            if (string.IsNullOrWhiteSpace(baseUsername))
            {
                baseUsername = "user";
            }

            int maximumLength = Math.Clamp(
                _registrationSettings.UsernameMaxLength,
                10,
                100);

            if (baseUsername.Length > maximumLength)
            {
                baseUsername =
                    baseUsername[..maximumLength];
            }

            bool baseUsernameExists =
                await _dbContext.Users.AnyAsync(
                    user =>
                        user.Username.ToLower() ==
                        baseUsername.ToLower(),
                    cancellationToken);

            if (!baseUsernameExists)
            {
                return baseUsername;
            }

            for (int attempt = 0; attempt < 20; attempt++)
            {
                string suffix =
                    RandomNumberGenerator
                        .GetInt32(0, 1_000_000)
                        .ToString("D6");

                int prefixLength =
                    maximumLength - suffix.Length;

                string shortenedBase =
                    baseUsername.Length > prefixLength
                        ? baseUsername[..prefixLength]
                        : baseUsername;

                string candidate =
                    shortenedBase + suffix;

                bool candidateExists =
                    await _dbContext.Users.AnyAsync(
                        user =>
                            user.Username.ToLower() ==
                            candidate.ToLower(),
                        cancellationToken);

                if (!candidateExists)
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException(
                "Không thể tạo username duy nhất.");
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        private static string GenerateOtp()
        {
            int number = RandomNumberGenerator.GetInt32(
                0,
                1_000_000);

            return number.ToString("D6");
        }

        private string HashOtp(
            string email,
            string otp)
        {
            byte[] secretBytes =
                Encoding.UTF8.GetBytes(
                    _otpSettings.Secret);

            byte[] contentBytes =
                Encoding.UTF8.GetBytes(
                    $"{email}:{otp}");

            using var hmac =
                new HMACSHA256(secretBytes);

            byte[] hashBytes =
                hmac.ComputeHash(contentBytes);

            return Convert.ToHexString(hashBytes);
        }

        private bool VerifyOtpHash(
            string storedHash,
            string email,
            string suppliedOtp)
        {
            try
            {
                string suppliedHash = HashOtp(
                    email,
                    suppliedOtp);

                byte[] storedBytes =
                    Convert.FromHexString(storedHash);

                byte[] suppliedBytes =
                    Convert.FromHexString(suppliedHash);

                return CryptographicOperations.FixedTimeEquals(
                    storedBytes,
                    suppliedBytes);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(
                    _otpSettings.Secret) ||
                Encoding.UTF8.GetByteCount(
                    _otpSettings.Secret) < 32)
            {
                throw new InvalidOperationException(
                    "Otp:Secret phải có ít nhất 32 byte.");
            }

            if (_otpSettings.LifetimeMinutes <= 0)
            {
                throw new InvalidOperationException(
                    "Otp:LifetimeMinutes phải lớn hơn 0.");
            }

            if (_registrationSettings.DefaultRoleId == 0)
            {
                throw new InvalidOperationException(
                    "Registration:DefaultRoleId chưa được cấu hình.");
            }
        }
    }
}
