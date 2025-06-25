using dispatch_app.Models.User;
using MailKit.Net.Smtp;
using MimeKit;

namespace dispatch_app.Utils
{
    public class EmailUtil
    {
        private readonly SmtpSettings _smtpSettings;

        public EmailUtil(SmtpSettings smtpSettings)
        {
            _smtpSettings = smtpSettings ?? throw new ArgumentNullException(nameof(smtpSettings));
        }

        public async Task<(bool Success, string ErrorMessage)> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
                email.To.Add(new MailboxAddress("", toEmail));
                email.Subject = subject;
                email.Body = new TextPart("html") { Text = body };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(_smtpSettings.Server, _smtpSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, $"Error al enviar el correo: {ex.Message}");
            }
        }
    }
}