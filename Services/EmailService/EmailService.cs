using Microsoft.AspNetCore.DataProtection;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace BelotWebApp.Services.EmailService
{
    public class EmailService : IEmailSender
    {

        byte[] a = { 141, 191, 67, 180, 133, 100, 99, 21, 74, 108, 9, 170, 177, 53, 131, 154 };

        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
            //EncryptPassword();
        }

        public async Task SendEmailAsync(string toAddress, string subject, string message)
        {
            MailMessage mailMessage = new MailMessage
            {
                Subject = subject,
                Body = message,
                From = new MailAddress(_config["SMTPConfiguration:SenderAddress"], _config["SMTPConfiguration:SenderDisplayName"]),
                IsBodyHtml = true
            };
            mailMessage.To.Add(toAddress);

            try
            {
                using (SmtpClient client = new SmtpClient(_config["SMTPConfiguration:Host"], int.Parse(_config["SMTPConfiguration:Port"]))
                {
                    Credentials = new NetworkCredential(_config["SMTPConfiguration:SenderAddress"], DecryptPassword(_config["SMTPConfiguration:EncryptedPassword"]).Result),
                    EnableSsl = true
                })
                {
                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Email send failure", ex);
            }
        }

        private async void EncryptPassword()
        {
            string encryptedPassword;

            string[] code = File.ReadAllLines(_config["EncrpytionKeyPath:Path"]);
            using (Aes aes = Aes.Create())
            {
                aes.IV = code[0].Split(',').Select(b => byte.Parse(b)).ToArray();
                aes.Key = Convert.FromBase64String(code[1]);
                using (MemoryStream output = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        await cryptoStream.WriteAsync(Encoding.Unicode.GetBytes(""));
                        await cryptoStream.FlushFinalBlockAsync();
                    }
                    encryptedPassword = Convert.ToBase64String(output.ToArray());
                }
            }
            string decryptedPassword = DecryptPassword(encryptedPassword).Result;
        }

        private async Task<string> DecryptPassword(string encrypedPassword)
        {
            string[] code = File.ReadAllLines(_config["EncrpytionKeyPath:Path"]);
            using (Aes aes = Aes.Create())
            {
                aes.IV = code[0].Split(',').Select(b => byte.Parse(b)).ToArray();
                aes.Key = Convert.FromBase64String(code[1]);
                using (MemoryStream input = new MemoryStream(Convert.FromBase64String(encrypedPassword)))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (MemoryStream output = new MemoryStream())
                        {
                            await cryptoStream.CopyToAsync(output);
                            return Encoding.Unicode.GetString(output.ToArray());
                        }
                    }
                }
            }
        }
    }
}
