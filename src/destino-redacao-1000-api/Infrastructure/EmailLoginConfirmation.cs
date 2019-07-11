using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace destino_redacao_1000_api
{
    public class EmailLoginConfirmation : IEmailLoginConfirmation
    {
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public EmailLoginConfirmation(IConfiguration configuration, IEmailSender emailSender)
        {
            _configuration = configuration;
            _emailSender = emailSender;
        }

        public async Task<bool> SendAsync(string email, string confirmationUrl, bool hasPasswordChanged)
        {
            string imgPath = _configuration["Website:BaseAddress"] + _configuration["Website:Logo"];
            StringBuilder body = new StringBuilder();
            body.AppendLine("<html><body>");
            body.AppendLine("<div align=\"center\">");
            body.AppendLine($"<img src=\"cid:logoImg\">");

            if (hasPasswordChanged) 
            {
                body.AppendLine("<h1>Olá</h1>");
                body.AppendLine($"<h4>Para confirmar a mudança de sua senha de acesso clique <a href='{ confirmationUrl }'>aqui</a></h4>");
            }
            else
            {
                body.AppendLine("<h1>Bem vindo ao nosso portal!</h1>");
                body.AppendLine($"<h3>Clique <a href='{ confirmationUrl }'>aqui</a> para completar seu cadastro.</h3>");
            }

            body.AppendLine("</div>");
            body.AppendLine("</body></html>");
            return await _emailSender.SendEmailAsync(email, "Confirme seu e-mail", body.ToString());
        }
    }
}