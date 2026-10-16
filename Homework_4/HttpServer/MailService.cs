using MimeKit;
using MailKit.Net.Smtp;
using System.Threading.Tasks;
namespace SocialApp.Services
{
    public class EmailService
    {
        // Доделать from.add,to.add, соединение с сервисом почты и аутентификацию
        public async Task SendEmailAsync(string email, string subject, string message, string path)
        {
            var builder = new BodyBuilder();
            builder.Attachments.Add(path);
            builder.TextBody = message;


            using var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Администрация сайта", "login@yandex.ru"));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = subject;
            emailMessage.Body = builder.ToMessageBody ();
             
            using (var client = new SmtpClient())
            {
                await client.ConnectAsync("smtp.yandex.ru", 25, false);
                await client.AuthenticateAsync("login@yandex.ru", "password");
                await client.SendAsync(emailMessage);
 
                await client.DisconnectAsync(true);
            }
        }
    }
}
