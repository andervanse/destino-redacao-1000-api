using System.ComponentModel.DataAnnotations;

namespace destino_redacao_1000_api
{
    public class CadastroUsuarioViewModel
    {
        [EmailAddress(ErrorMessage="Email inválido")]
        [Required(ErrorMessage="Campo obrigatório")]
        [MinLength(8, ErrorMessage="Tamanho mínimo de 8 caracteres")]
        public string Login { get; set; }

        [MinLength(4, ErrorMessage="Tamanho mínimo de 4 caracteres")]
        public string Nome { get; set; }

        [Required(ErrorMessage="Campo obrigatório")]
        [MinLength(8, ErrorMessage="Tamanho mínimo de 8 caracteres")]
        public string Senha { get; set; }

        [Required(ErrorMessage="Campo obrigatório")]
        [Compare("Senha", ErrorMessage="Senhas diferentes")]
        [MinLength(8, ErrorMessage="Tamanho mínimo de 8 caracteres")]
        public string ConfirmaSenha { get; set; }

        public string CodigoResetSenha { get; set; }        
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