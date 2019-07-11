using System.Threading.Tasks;

namespace destino_redacao_1000_api
{
    public interface IEmailSender
    {
        Task<bool> SendEmailAsync(string email, string subject, string message);
    }
}