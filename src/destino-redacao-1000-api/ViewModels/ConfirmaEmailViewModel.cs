using System.ComponentModel.DataAnnotations;

namespace destino_redacao_1000_api
{
    public class ConfirmaEmailViewModel
    {
        [EmailAddress(ErrorMessage="E-mail inválido.")]
        [Required(ErrorMessage="E-mail Obrigatório.")]
        public string Email { get; set; }

        [Required(ErrorMessage="Código Obrigatório.")]
        public string Codigo { get; set; }
    }
}