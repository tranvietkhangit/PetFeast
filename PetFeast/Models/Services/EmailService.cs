using System.Net;
using System.Net.Mail;

namespace PetFeast.Models.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ==========================================
        // GỬI OTP ĐẶT LẠI MẬT KHẨU
        // ==========================================

        public async Task SendResetOtpEmailAsync(
    string email,
    string otp)
        {
            var smtpHost =
                _configuration["EmailSettings:SmtpHost"];

            var smtpPort =
                int.Parse(
                    _configuration["EmailSettings:SmtpPort"] ?? "587");

            var smtpEmail =
                _configuration["EmailSettings:Email"];

            var smtpPassword =
                _configuration["EmailSettings:Password"];

            using var message = new MailMessage();

            message.From = new MailAddress(
                smtpEmail!,
                "PetFeast");

            message.To.Add(email);

            message.Subject =
                "PetFeast - Mã OTP đặt lại mật khẩu";

            message.IsBodyHtml = true;

            message.Body = $@"
<!DOCTYPE html>
<html>
<body style='font-family:Arial,sans-serif;'>

    <h2 style='color:#0d6efd;'>
        PetFeast
    </h2>

    <p>Xin chào,</p>

    <p>
        Bạn vừa yêu cầu đặt lại mật khẩu
        cho tài khoản PetFeast.
    </p>

    <p>
        Mã OTP của bạn là:
    </p>

    <div style='
        font-size:32px;
        font-weight:bold;
        letter-spacing:8px;
        color:#0d6efd;
        margin:20px 0;
    '>
        {otp}
    </div>

    <p>
        Mã OTP có hiệu lực trong
        <strong>5 phút</strong>.
    </p>

    <p>
        Không chia sẻ mã OTP này cho bất kỳ ai.
    </p>

    <p>
        Nếu bạn không yêu cầu đặt lại mật khẩu,
        hãy bỏ qua email này.
    </p>

    <hr>

    <p>
        Trân trọng,<br>
        <strong>PetFeast</strong>
    </p>

</body>
</html>";

            using var smtp = new SmtpClient(
                smtpHost,
                smtpPort);

            smtp.EnableSsl = true;

            smtp.Credentials =
                new NetworkCredential(
                    smtpEmail,
                    smtpPassword);

            await smtp.SendMailAsync(message);
        }
    }
}
