
using System.ComponentModel.DataAnnotations;

namespace destino_redacao_1000_api
{
    public class RedefinicaoSenhaViewModel
    {
        [EmailAddress(ErrorMessage="E-mail inválido.")]
        [Required(ErrorMessage="E-mail Obrigatório.")]
        public string Email { get; set; }
    }
}