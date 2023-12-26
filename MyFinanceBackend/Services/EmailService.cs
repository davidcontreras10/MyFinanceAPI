using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace MyFinanceBackend.Services
{
    public class EmailService : IEmailService
    {
        #region IEmailService Implementation

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            var from = GetDefaultEmailAddress();
            var credential = GetDefaultEmailAddressCredential();
            var result = await SendEmailAsync(from, to, credential, subject, body, isHtml);
            return result;
        }

        #endregion

        #region Private Methods 

        private static string GetDefaultEmailAddress()
        {
            return "myfinancecontact@gmail.com";
        }

        private static string GetDefaultEmailAddressCredential()
        {
            return "mmrijlljcdcicfgn"; 
        }

        private async Task<bool> SendEmailAsync(string from, string to, string credential, string subject, string body, bool isHtml)
        {
            var client = GetGmailSmtpClient(from, credential);
            var message = new MailMessage(from, to, subject, body) {IsBodyHtml = isHtml};
            client.Send(message);
            return await Task.FromResult(true);
        }

        private SmtpClient GetGmailSmtpClient(string from,string credential)
        {
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(from, credential),
                EnableSsl = true,
            };

            client.SendCompleted += EmailSent;
            return client;
        }

        private void EmailSent(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Debug.WriteLine(e?.Error);
        }   

        #endregion
    }
}
