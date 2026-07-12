using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace ORBIS.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly SmtpSettings _settings;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(
            IOptions<SmtpSettings> options,
            ILogger<SmtpEmailService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }
        public async Task SendRegistrationOtpAsync(string receiverEmail, string otp, int lifetimeMinutes, CancellationToken cancellationToken = default)
        {
            ValidateSettings();

            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    _settings.FromName,
                    _settings.FromEmail));

            message.To.Add(
                MailboxAddress.Parse(receiverEmail));

            message.Subject = "Mã OTP xác thực tài khoản ORBIS";

            message.Body = new TextPart(TextFormat.Html)
            {
                Text = $"""
                <div style="
                    max-width: 560px;
                    margin: auto;
                    padding: 24px;
                    font-family: Arial, sans-serif;
                    border: 1px solid #dddddd;
                    border-radius: 10px;">

                    <h2>Xác thực tài khoản ORBIS</h2>

                    <p>Mã OTP đăng ký của bạn là:</p>

                    <div style="
                        margin: 24px 0;
                        padding: 16px;
                        text-align: center;
                        background: #f3f4f6;
                        border-radius: 8px;
                        font-size: 32px;
                        font-weight: bold;
                        letter-spacing: 8px;">
                        {otp}
                    </div>

                    <p>
                        Mã có hiệu lực trong
                        <strong>{lifetimeMinutes} phút</strong>.
                    </p>

                    <p>Không chia sẻ mã OTP này với người khác.</p>

                    <p style="color: #666666; font-size: 13px;">
                        Nếu bạn không thực hiện đăng ký,
                        hãy bỏ qua email này.
                    </p>
                </div>
                """
            };

            using var smtpClient = new SmtpClient();

            try
            {
                SecureSocketOptions securityOption =
                    _settings.Port == 465
                        ? SecureSocketOptions.SslOnConnect
                        : SecureSocketOptions.StartTls;

                await smtpClient.ConnectAsync(
                    _settings.Host,
                    _settings.Port,
                    securityOption,
                    cancellationToken);

                await smtpClient.AuthenticateAsync(
                    _settings.Username,
                    _settings.Password,
                    cancellationToken);

                await smtpClient.SendAsync(
                    message,
                    cancellationToken);

                await smtpClient.DisconnectAsync(
                    true,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Gửi OTP thất bại đến email {Email}",
                    receiverEmail);

                throw;
            }
        }

        private void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(_settings.Host) ||
                string.IsNullOrWhiteSpace(_settings.Username) ||
                string.IsNullOrWhiteSpace(_settings.Password) ||
                string.IsNullOrWhiteSpace(_settings.FromEmail))
            {
                throw new InvalidOperationException(
                    "Cấu hình SMTP chưa đầy đủ.");
            }
        }
    }
}
