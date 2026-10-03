using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Net.Mail;

namespace ApiCore8.Infrastructure;

public class EmailService
{
    public static bool SendEmail(
        string smtpHost,
        int smtpPort,
        string smtpUser,
        string smtpPassword,
        string fromEmail,
        string fromName,
        string toEmail,
        string subject,
        string body,
        bool isBodyHtml = true)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));

            foreach (var to in toEmail.Split(','))
            {
                if (!string.IsNullOrWhiteSpace(to))
                    message.To.Add(MailboxAddress.Parse(to.Trim()));
            }

            message.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            if (isBodyHtml)
                bodyBuilder.HtmlBody = body;
            else
                bodyBuilder.TextBody = body;
            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                client.Connect(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                client.Authenticate(smtpUser, smtpPassword);
                client.Send(message);
                client.Disconnect(true);
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Lỗi gửi mail: " + ex.Message);
            return false;
        }
    }
}