using System.ComponentModel.DataAnnotations;

namespace destino_redacao_1000_api
{
    public class LoginUsuarioViewModel
    {
        [EmailAddress(ErrorMessage="Email inválido")]
        [Required(ErrorMessage="Campo obrigatório")]
        [MinLength(8, ErrorMessage="Tamanho mínimo de 8 caracteres")]
        public string Login { get; set; }

        [MinLength(8, ErrorMessage="Tamanho mínimo de 8 caracteres")]
        public string Nome { get; set; }

        [Required(ErrorMessage="Campo obrigatório")]
        [MinLength(8, ErrorMessage="Tamanho mínimo de 8 caracteres")]
        public string Senha { get; set; }

        [Compare("Senha", ErrorMessage="Senhas diferentes")]
        public string ConfirmaSenha { get; set; }
    }

    public class CredenciaisUsuarioViewModel
    {
        [EmailAddress(ErrorMessage="Email inválido")]
        [Required(ErrorMessage="Campo obrigatório")]
        [MinLength(8, ErrorMessage="Tamanho mínimo de 8 caracteres")]
        public string Login { get; set; }

        [Required(ErrorMessage="Campo obrigatório")]
        [MinLength(8, ErrorMessage="Tamanho mínimo de 8 caracteres")]
        public string Senha { get; set; }
    }    
}    