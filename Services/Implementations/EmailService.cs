using MailKit.Net.Smtp;
using MimeKit;
using OSBIS.Models.Entities;
using OSBIS.Services.Interfaces;
using Serilog;

namespace OSBIS.Services.Implementations
{
    /// <summary>
    /// EmailService — Phase 5 (đã nâng cấp từ placeholder lên MailKit SMTP thật).
    ///
    /// Cách hoạt động:
    /// - Đọc cấu hình từ appsettings.json section "Email" (SmtpServer, SmtpPort, SmtpUsername, SmtpPassword, FromEmail, FromName).
    /// - Nếu SmtpUsername == "your-email@gmail.com" (placeholder) → fallback về log Serilog (môi trường dev không có SMTP thật).
    /// - Nếu send thất bại (timeout, auth fail,...) → log lỗi nhưng KHÔNG throw (fire-and-forget).
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        private string SmtpServer => _config["Email:SmtpServer"] ?? "smtp.gmail.com";
        private int SmtpPort => int.TryParse(_config["Email:SmtpPort"], out var p) ? p : 587;
        private string SmtpUsername => _config["Email:SmtpUsername"] ?? "";
        private string SmtpPassword => _config["Email:SmtpPassword"] ?? "";
        private string FromEmail => _config["Email:FromEmail"] ?? "noreply@OSBIS.com";
        private string FromName => _config["Email:FromName"] ?? "OSBIS";

        /// <summary>Kiểm tra nếu SMTP đã được cấu hình đầy đủ (không phải placeholder).</summary>
        private bool IsSmtpConfigured()
        {
            return !string.IsNullOrWhiteSpace(SmtpUsername)
                && !string.IsNullOrWhiteSpace(SmtpPassword)
                && !SmtpUsername.Contains("your-email", StringComparison.OrdinalIgnoreCase);
        }

        public async Task SendOrderConfirmationAsync(Order order)
        {
            var subject = $"[OSBIS] Xác nhận đơn hàng #{order.OrderCode}";
            var body = $@"
<html><body style='font-family: Arial;'>
<h2 style='color: #d63384;'>Cảm ơn bạn đã đặt hàng tại OSBIS!</h2>
<p>Đơn hàng <strong>{order.OrderCode}</strong> đã được tạo thành công với tổng giá trị <strong>{order.TotalAmount:N0}đ</strong>.</p>
<ul>
    <li>Phí vận chuyển: {order.ShippingFee:N0}đ</li>
    <li>Giảm giá: {order.DiscountAmount:N0}đ</li>
    <li>Tổng cộng: <strong>{order.TotalAmount:N0}đ</strong></li>
</ul>
<p>Chúng tôi sẽ liên hệ với bạn trong thời gian sớm nhất.</p>
<p>Trân trọng,<br/>Đội ngũ OSBIS</p>
</body></html>";

            await SendAsync(order.User?.Email ?? "", subject, body, $"OrderConfirmation-{order.OrderCode}");
        }

        public async Task SendShippingUpdateAsync(Order order, string statusNote)
        {
            var subject = $"[OSBIS] Cập nhật vận chuyển #{order.OrderCode}";
            var body = $@"
<html><body style='font-family: Arial;'>
<h2 style='color: #0d6efd;'>Cập nhật vận chuyển</h2>
<p>Đơn hàng <strong>{order.OrderCode}</strong>: {statusNote}</p>
<p>Bạn có thể theo dõi đơn hàng chi tiết tại trang chủ OSBIS.</p>
<p>Trân trọng,<br/>Đội ngũ OSBIS</p>
</body></html>";

            await SendAsync(order.User?.Email ?? "", subject, body, $"ShippingUpdate-{order.OrderCode}");
        }

        public async Task SendPaymentConfirmationAsync(Payment payment)
        {
            var orderCode = payment.Order?.OrderCode ?? $"#{payment.OrderId}";
            var subject = $"[OSBIS] Xác nhận thanh toán đơn hàng {orderCode}";
            var body = $@"
<html><body style='font-family: Arial;'>
<h2 style='color: #198754;'>Thanh toán thành công</h2>
<p>Đơn hàng <strong>{orderCode}</strong> đã được thanh toán thành công với số tiền <strong>{payment.Amount:N0}đ</strong>.</p>
<p>Phương thức: <strong>{payment.PaymentMethod}</strong></p>
<p>Mã giao dịch: <code>{payment.ProviderTransactionId}</code></p>
<p>Cảm ơn bạn đã mua hàng!</p>
<p>Trân trọng,<br/>Đội ngũ OSBIS</p>
</body></html>";

            await SendAsync(payment.Order?.User?.Email ?? "", subject, body, $"PaymentConfirmation-{orderCode}");
        }

        public async Task SendVoucherCodeAsync(User user, Voucher voucher)
        {
            var subject = $"[OSBIS] Bạn có voucher mới: {voucher.VoucherCode}";
            var body = $@"
<html><body style='font-family: Arial;'>
<h2 style='color: #fd7e14;'>Voucher mới từ OSBIS!</h2>
<p>Xin chào <strong>{user.FullName}</strong>,</p>
<p>Bạn vừa nhận được voucher <strong>{voucher.VoucherCode}</strong>:</p>
<ul>
    <li>Giảm: {voucher.DiscountValue}{(voucher.DiscountType == Models.Enums.DiscountType.Percent ? "%" : "đ")}</li>
    <li>Đơn tối thiểu: {voucher.MinOrderValue:N0}đ</li>
    <li>Hiệu lực: {voucher.StartDate:dd/MM/yyyy} - {voucher.EndDate:dd/MM/yyyy}</li>
</ul>
<p>Sử dụng ngay tại <a href='/'>OSBIS</a>!</p>
<p>Trân trọng,<br/>Đội ngũ OSBIS</p>
</body></html>";

            await SendAsync(user.Email ?? "", subject, body, $"VoucherCode-{voucher.VoucherCode}");
        }

        // ============================================================
        // Helper: gửi email thật qua MailKit hoặc fallback log
        // ============================================================
        private async Task SendAsync(string toEmail, string subject, string htmlBody, string context)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                Log.Warning("[EMAIL] {Context}: no recipient email → skip", context);
                return;
            }

            // Fallback: nếu chưa cấu hình SMTP thật → chỉ log
            if (!IsSmtpConfigured())
            {
                Log.Information("[EMAIL-DEV-MODE] To: {To} | Subject: {Subject} | Context: {Context}",
                    toEmail, subject, context);
                return;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(FromName, FromEmail));
                message.To.Add(new MailboxAddress(toEmail, toEmail));
                message.Subject = subject;
                message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(SmtpServer, SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(SmtpUsername, SmtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                Log.Information("[EMAIL] Sent {Context} to {To}: {Subject}", context, toEmail, subject);
            }
            catch (Exception ex)
            {
                // KHÔNG throw - email fail không được làm fail flow chính
                Log.Error(ex, "[EMAIL] Failed to send {Context} to {To}: {Subject}", context, toEmail, subject);
            }
        }
    }
}