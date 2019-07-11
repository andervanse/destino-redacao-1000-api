using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace destino_redacao_1000_api
{
    public class ZohoEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public ILogger _log { get; }

        public ZohoEmailSender(IConfiguration configuration, ILoggerFactory logger)
        {
            _configuration = configuration;
            _log = logger.CreateLogger("ZohoEmailSender");
        }

        public Task<bool> SendEmailAsync(string email, string subject, string message)
        {
            String FROM = _configuration["Zoho:From"];
            String FROMNAME = _configuration["Zoho:FromName"];
            String SMTP_USERNAME = _configuration["Zoho:Username"];
            String SMTP_PASSWORD = _configuration["Zoho:Password"];
            String HOST = _configuration["Zoho:Host"];
            int PORT = int.Parse(_configuration["Zoho:Port"]);

            MailMessage msg = new MailMessage();
            msg.IsBodyHtml = true;
            msg.From = new MailAddress(FROM, FROMNAME);
            msg.To.Add(new MailAddress(email));
            msg.Subject = subject;
            msg.Body = message;

            AttachImage(msg);

            // Create and configure a new SmtpClient
            SmtpClient client = new SmtpClient(HOST, PORT);
            client.UseDefaultCredentials = false;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.Credentials = new NetworkCredential(SMTP_USERNAME, SMTP_PASSWORD);
            client.EnableSsl = true;

            bool threwException = false;
            try
            {
                client.Send(msg);
            }
            catch (Exception ex)
            {
                threwException = true;
                _log.LogError(ex.Message);
            }

            return Task.FromResult(threwException);
        }

        private void AttachImage(MailMessage msg)
        {
            try
            {
                var imgUrl = _configuration["Website:BaseAddress"] + _configuration["Website:Logo"];

                if (imgUrl != null)
                {
                    WebRequest req = WebRequest.Create(imgUrl);
                    WebResponse response = req.GetResponse();
                    Stream stream = response.GetResponseStream();
                    AlternateView avHtml = AlternateView.CreateAlternateViewFromString(msg.Body, null, MediaTypeNames.Text.Html);
                    LinkedResource logoResource = new LinkedResource(stream, MediaTypeNames.Image.Jpeg);
                    logoResource.ContentId = "logoImg";
                    avHtml.LinkedResources.Add(logoResource);
                    msg.AlternateViews.Add(avHtml);
                }
            }
            catch (System.Exception)
            {
                _log.LogError("Error while attaching image to e-mail.");
            }
        }
    }
}