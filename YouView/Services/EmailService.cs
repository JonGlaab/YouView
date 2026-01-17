using MailKit.Net.Smtp;
using MimeKit;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        var senderName = _config["EmailSettings:SenderName"] ?? "YouView";
        var senderEmail = _config["EmailSettings:SenderEmail"] ?? "no-reply@youview.com";

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(senderName, senderEmail));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = subject;

        email.Body = new TextPart("html")
        {
            Text = message
        };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(
            _config["EmailSettings:SmtpServer"],
            int.Parse(_config["EmailSettings:Port"] ?? "587"),
            false
        );

        await smtp.AuthenticateAsync(
            _config["EmailSettings:Username"],
            _config["EmailSettings:Password"]
        );

        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}