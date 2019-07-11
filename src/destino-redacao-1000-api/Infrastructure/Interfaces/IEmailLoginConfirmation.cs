using System.Threading.Tasks;

namespace destino_redacao_1000_api
{
    public interface IEmailLoginConfirmation
    {
        Task<bool> SendAsync(string email, string confirmationUrl, bool hasPasswordChanged);
    }
}