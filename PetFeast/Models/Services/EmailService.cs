using System.Net;
using System.Net.Mail;

namespace PetFeast.Models.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendResetPasswordEmailAsync(
            string email,
            string resetLink)
        {
            var smtpHost = _configuration["EmailSettings:SmtpHost"];
            var smtpPort = int.Parse(
                _configuration["EmailSettings:SmtpPort"] ?? "587");

            var smtpEmail = _configuration["EmailSettings:Email"];
            var smtpPassword = _configuration["EmailSettings:Password"];

            using var message = new MailMessage();

            message.From = new MailAddress(
                smtpEmail!,
                "PetFeast");

            message.To.Add(email);

            message.Subject = "PetFeast - Đặt lại mật khẩu";

            message.IsBodyHtml = true;

            message.Body = $@"
<!DOCTYPE html>
<html>
<body style='font-family: Arial, sans-serif;'>

    <h2 style='color:#0d6efd;'>
        PetFeast
    </h2>

    <p>Xin chào,</p>

    <p>
        Chúng tôi nhận được yêu cầu đặt lại mật khẩu
        cho tài khoản PetFeast của bạn.
    </p>

    <p>
        Nhấn vào nút bên dưới để đặt lại mật khẩu:
    </p>

    <p>
        <a href='{resetLink}'
           style='
               display:inline-block;
               padding:12px 20px;
               background:#0d6efd;
               color:white;
               text-decoration:none;
               border-radius:6px;
           '>
            Đặt lại mật khẩu
        </a>
    </p>

    <p>
        Nếu bạn không yêu cầu đặt lại mật khẩu,
        bạn có thể bỏ qua email này.
    </p>

    <p>
        Trân trọng,<br/>
        <strong>PetFeast</strong>
    </p>

</body>
</html>";

            using var smtp = new SmtpClient(
                smtpHost,
                smtpPort);

            smtp.EnableSsl = true;

            smtp.Credentials = new NetworkCredential(
                smtpEmail,
                smtpPassword);

            await smtp.SendMailAsync(message);
        }
    }
}
