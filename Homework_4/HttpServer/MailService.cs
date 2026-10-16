using MimeKit;
using MailKit.Net.Smtp;
using System.Threading.Tasks;
using Search_server;
using MailKit.Security;
namespace SocialApp.Services
{
    public class EmailService
    {
        public string FromEmail{get;private set;}
        public string Password{get;private set;}
        public EmailService(string fromEmail, string password)
        {
            FromEmail = fromEmail;
            Password = password;
        }
        // Доделать from.add,to.add, соединение с сервисом почты и аутентификацию
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
