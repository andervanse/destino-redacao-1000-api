using System.ComponentModel.DataAnnotations;

namespace destino_redacao_1000_api
{
    public class CredencialUsuario
    {
        [Required]
        public string Login { get; set; }

        [Required]
        [MinLength(8)]
        public string Senha { get; set; }

        [Compare("Password")]
        public string ConfirmaSenha { get; set; }
    }

    public class CredencialLogin
    {
        [Required]
        public string Login { get; set; }

        [Required]
        [MinLength(8)]
        public string Senha { get; set; }
    }    

}    