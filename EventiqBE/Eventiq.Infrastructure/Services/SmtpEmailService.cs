using System.Net;
using System.Net.Mail;
using Eventiq.Application.Interfaces.Services;

namespace Eventiq.Infrastructure.Services;

public class SmtpEmailService(string? emailFrom, string sandboxSmtpMailtrapIo, int port, string? mailtrapUsername, string? mailtrapPassword) :IEmailService
{
    public Task SendEmailAsync(string to, string subject, string body)
    {
        var client = new SmtpClient(sandboxSmtpMailtrapIo, port)
        {
            Credentials = new NetworkCredential(mailtrapUsername, mailtrapPassword),
            EnableSsl = true
        };
        if (emailFrom != null) client.Send(emailFrom, to, subject, body);
        return Task.CompletedTask;
    }
}