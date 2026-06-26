using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using HungStore.Application.Notifications.Interfaces;

namespace HungStore.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;

    public SmtpEmailSender(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendAsync(string toEmail, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host) || string.IsNullOrWhiteSpace(_settings.User))
        {
            // SMTP not configured (e.g. local dev/test without user-secrets) — skip rather than
            // attempting a real network connection to a placeholder host.
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
        await client.AuthenticateAsync(_settings.User, _settings.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
