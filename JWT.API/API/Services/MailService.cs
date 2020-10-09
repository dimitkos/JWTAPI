using API.Constants;
using API.Models;
using API.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace API.Services
{
    public class MailService : IMailService
    {
        private readonly MailSettings _mailSettings;
        public MailService(IOptions<MailSettings> mailSettings)
        {
            _mailSettings = mailSettings.Value;
        }

        public async Task SendEmailAsync(MailRequest mailRequest)
        {
            var builder = CreateBody(mailRequest.Attachments);
            builder.HtmlBody = mailRequest.Body;

            var email = CreateEmail(mailRequest, builder);

            await Send(email);
        }

        public async Task SendCustomEmailAsync(RegisterModel request, string subject)
        {
            var mailtext = LoadTemplate(LoadTemplatePaths.WelcomeTemplate);
            mailtext = mailtext.Replace("[username]", request.Username).Replace("[email]", request.Email);

            var mailRequest = new MailRequest
            {
                ToEmail = request.Email,
                Subject = subject
            };

            var builder = CreateBody(mailRequest.Attachments);
            builder.HtmlBody = mailtext;

            var email = CreateEmail(mailRequest, builder);

            await Send(email);
        }

        private MimeMessage CreateEmail(MailRequest mailRequest, BodyBuilder builder)
        {
            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(_mailSettings.Mail);
            email.To.Add(MailboxAddress.Parse(mailRequest.ToEmail));
            email.Subject = mailRequest.Subject;
            email.Body = builder.ToMessageBody();

            return email;
        }

        private BodyBuilder CreateBody(List<IFormFile> attachments)
        {
            var builder = new BodyBuilder();
            if (attachments != null)
            {
                byte[] fileBytes;

                foreach (var file in attachments)
                {
                    if (file.Length > 0)
                    {
                        using (var ms = new MemoryStream())
                        {
                            file.CopyTo(ms);
                            fileBytes = ms.ToArray();
                        }
                        builder.Attachments.Add(file.FileName, fileBytes, ContentType.Parse(file.ContentType));
                    }
                }
            }

            return builder;
        }

        private async Task Send(MimeMessage email)
        {
            using var smtp = new SmtpClient();
            smtp.Connect(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);
            smtp.Authenticate(_mailSettings.Mail, _mailSettings.Password);
            await smtp.SendAsync(email);
            smtp.Disconnect(true);
        }

        private string LoadTemplate(string path)
        {
            string FilePath = Directory.GetCurrentDirectory() + path;
            StreamReader str = new StreamReader(FilePath);
            string mailText = str.ReadToEnd();
            str.Close();

            return mailText;
        }
    }
}
