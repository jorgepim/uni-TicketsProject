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

        public string GenerarCuerpoCorreo(string ticketId, string titulo, string estadoAnterior, string estadoNuevo, string descripcion)
        {
            return $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; }}
                    .header {{ background-color: #4CAF50; color: white; padding: 10px; text-align: center; }}
                    .content {{ padding: 20px; }}
                    .footer {{ text-align: center; font-size: 0.9em; color: #aaa; }}
                </style>
            </head>
            <body>
                <div class='header'>
                    <h2>Cambio de Estado del Ticket #{ticketId}</h2>
                </div>
                <div class='content'>
                    <p><strong>Título:</strong> {titulo}</p>
                    <p><strong>Descripción:</strong> {descripcion}</p>
                    <p><strong>Estado Anterior:</strong> {estadoAnterior}</p>
                    <p><strong>Nuevo Estado:</strong> {estadoNuevo}</p>
                </div>
                <div class='footer'>
                    <p>Gracias por utilizar nuestro sistema de tickets.</p>
                </div>
            </body>
            </html>";
        }
        public string GenerarCuerpoCorreoComentario(string ticketId, string titulo, string comentario, string usuarioNombre)
        {
            return $@"
    <html>
    <head>
        <style>
            body {{ font-family: Arial, sans-serif; }}
            .header {{ background-color: #007bff; color: white; padding: 10px; text-align: center; }}
            .content {{ padding: 20px; }}
            .footer {{ text-align: center; font-size: 0.9em; color: #aaa; }}
        </style>
    </head>
    <body>
        <div class='header'>
            <h2>Nuevo Comentario en el Ticket #{ticketId}</h2>
        </div>
        <div class='content'>
            <p><strong>Título:</strong> {titulo}</p>
            <p><strong>Comentario:</strong> {comentario}</p>
            <p><strong>Comentado por:</strong> {usuarioNombre}</p>
        </div>
        <div class='footer'>
            <p>Gracias por utilizar nuestro sistema de tickets. Si tienes alguna pregunta, no dudes en ponerte en contacto con nosotros.</p>
        </div>
    </body>
    </html>";
        }
        public string GenerarCuerpoCorreoAdjunto(string ticketId, string titulo, string archivoNombre, string usuarioNombre)
        {
            return $@"
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; }}
        .header {{ background-color: #28a745; color: white; padding: 10px; text-align: center; }}
        .content {{ padding: 20px; }}
        .footer {{ text-align: center; font-size: 0.9em; color: #aaa; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2>Nuevo Archivo Adjunto en el Ticket #{ticketId}</h2>
    </div>
    <div class='content'>
        <p><strong>Título:</strong> {titulo}</p>
        <p><strong>Archivo Adjunto:</strong> {archivoNombre}</p>
        <p><strong>Subido por:</strong> {usuarioNombre}</p>
    </div>
    <div class='footer'>
        <p>Gracias por utilizar nuestro sistema de tickets. Si tienes alguna pregunta, no dudes en ponerte en contacto con nosotros.</p>
    </div>
</body>
</html>";
        }



    }
}
