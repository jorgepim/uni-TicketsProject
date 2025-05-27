using System.Net;
using System.Net.Mail;

namespace TicketsApp.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            var smtpClient = new SmtpClient(emailSettings["Host"])
            {
                Port = int.Parse(emailSettings["Port"]),
                Credentials = new NetworkCredential(emailSettings["UserName"], emailSettings["Password"]),
                EnableSsl = bool.Parse(emailSettings["EnableSsl"])
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(emailSettings["UserName"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Maneja cualquier error de envío de correo
                Console.WriteLine($"Error enviando el correo: {ex.Message}");
            }
        }
    }
}
