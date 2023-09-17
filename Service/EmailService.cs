using BelotWebApp.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;

namespace BelotWebApp.Service
{
    public class EmailService// : IEmailService
    {
        private readonly string templatePath = HostingEnvironment.MapPath(@"/EmailTemplate/{0}.html");

        public EmailService()
        {

        }

        public async Task SendTestEmail(UserEmailOptions userEmailOptions)
        {
            userEmailOptions.Subject = "NFTest";
            userEmailOptions.Body = GetEmailBody("ConfirmEmail");

            await SendEmail(userEmailOptions);
        }

        private async Task SendEmail(UserEmailOptions userEmailOptions)
        {
            MailMessage mail = new MailMessage
            {
                Subject = userEmailOptions.Subject,
                Body = userEmailOptions.Body,
                From = new MailAddress(ConfigurationManager.AppSettings["senderEmailId"], ConfigurationManager.AppSettings["senderEmailName"]),
                IsBodyHtml = true
            };

            foreach (string toEmail in userEmailOptions.ToEmails)
            {
                mail.To.Add(toEmail);
            }

            NetworkCredential networkCredential = new NetworkCredential(ConfigurationManager.AppSettings["senderEmailId"], ConfigurationManager.AppSettings["senderEmailPassword"]);

            SmtpClient smtpClient = new SmtpClient
            {
                Host = ConfigurationManager.AppSettings["senderSMPTClient"],
                Port = int.Parse(ConfigurationManager.AppSettings["senderSMPTPort"]),
                Credentials = networkCredential,
                EnableSsl = true
            };

            mail.BodyEncoding = Encoding.Default;

            await smtpClient.SendMailAsync(mail);
        }

        private string GetEmailBody(string templateName)
        {
            string body = File.ReadAllText(string.Format(templatePath, templateName));
            return body;
        }
    }
}