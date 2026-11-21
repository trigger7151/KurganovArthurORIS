using MimeKit;
using MailKit.Net.Smtp;
using System.Threading.Tasks;
using MiniHttpServer.Framework.Content;
using MailKit.Security;
namespace HttpServer
{
    /// <summary>
    /// Отправляет письма с вложением через SMTP-сервер Gmail.
    /// </summary>
    public class EmailService
    {
        /// <summary>
        /// Адрес отправителя.
        /// </summary>
        public string FromEmail{get;private set;}
        /// <summary>
        /// Пароль для входа на SMTP-сервер.
        /// </summary>
        public string Password{get;private set;}
        /// <summary>
        /// Создаёт сервис отправки писем от имени указанного ящика.
        /// </summary>
        /// <param name="fromEmail">Адрес отправителя.</param>
        /// <param name="password">Пароль для входа на SMTP-сервер.</param>
        public EmailService(string fromEmail, string password)
        {
            FromEmail = fromEmail;
            Password = password;
        }
        /// <summary>
        /// Отправляет письмо с вложением <c>server.zip</c>.
        /// </summary>
        /// <param name="email">Адрес получателя.</param>
        /// <param name="subject">Тема письма.</param>
        /// <param name="message">Текст письма.</param>
        /// <param name="stream">Содержимое вложения.</param>
        /// <returns>Задача, завершающаяся после отправки письма.</returns>
        public async Task SendEmailAsync(string email, string subject, string message, Stream stream)
        {
            var builder = new BodyBuilder();
            // builder.Attachments.Add(filePath);
            builder.TextBody = message;
            builder.Attachments.Add("server.zip", stream);

            using var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("", FromEmail));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = subject;
            emailMessage.Body = builder.ToMessageBody ();
             
            using (var client = new SmtpClient())
            {
                await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(FromEmail, Password);
                await client.SendAsync(emailMessage);
 
                await client.DisconnectAsync(true);
            }
        }
    }
}
