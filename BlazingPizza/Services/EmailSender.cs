using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System;

namespace BlazingPizza.Services
{
    public class AuthMessageSenderOptions
    {
        public string? SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string? SenderName { get; set; }
        public string? SenderEmail { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public class EmailSender<TUser> : IEmailSender<TUser> where TUser : class
    {
        private readonly AuthMessageSenderOptions _options;

        public EmailSender(IOptions<AuthMessageSenderOptions> optionsAccessor)
        {
            _options = optionsAccessor.Value ?? throw new ArgumentNullException(nameof(optionsAccessor), "AuthMessageSenderOptions must be provided.");
            if (string.IsNullOrEmpty(_options.SmtpServer)) throw new ArgumentNullException(nameof(_options.SmtpServer), "SMTP server must be provided.");
            if (string.IsNullOrEmpty(_options.SenderEmail)) throw new ArgumentNullException(nameof(_options.SenderEmail), "Sender email must be provided.");
            if (string.IsNullOrEmpty(_options.Username)) throw new ArgumentNullException(nameof(_options.Username), "SMTP username must be provided.");
            if (string.IsNullOrEmpty(_options.Password)) throw new ArgumentNullException(nameof(_options.Password), "SMTP password must be provided.");
        }

        private async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_options.SenderName, _options.SenderEmail));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
            emailMessage.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_options.SmtpServer, _options.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_options.Username, _options.Password);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
        }

        public async Task SendConfirmationLinkAsync(TUser user, string email, string confirmationLink)
        {
            var subject = "Confirm your email";
            var htmlMessage = $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.";
            await SendEmailAsync(email, subject, htmlMessage);
        }

        public async Task SendPasswordResetLinkAsync(TUser user, string email, string resetLink)
        {
            var subject = "Reset your password";
            var htmlMessage = $"You can reset your password by <a href='{resetLink}'>clicking here</a>.";
            await SendEmailAsync(email, subject, htmlMessage);
        }

        public async Task SendPasswordResetCodeAsync(TUser user, string email, string code)
        {
            var subject = "Reset your password";
            var htmlMessage = $"Your password reset code is: {code}";
            await SendEmailAsync(email, subject, htmlMessage);
        }
    }
}