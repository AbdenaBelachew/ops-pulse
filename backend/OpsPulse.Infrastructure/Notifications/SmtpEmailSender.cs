using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using OpsPulse.Infrastructure.Monitoring;

namespace OpsPulse.Infrastructure.Notifications;

public interface IEmailSender
{
    bool IsConfigured { get; }

    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}

/// <summary>MailKit-based SMTP sender. When SMTP is not configured, callers skip email delivery.</summary>
public sealed class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions _smtp = options.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_smtp.Host);

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("SMTP is not configured (Smtp:Host is empty).");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.Auto, cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_smtp.UserName))
            {
                await client.AuthenticateAsync(_smtp.UserName, _smtp.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            logger.LogInformation("Notification email sent to {To}", to);
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}
