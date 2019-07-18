
using System;

namespace destino_redacao_1000_api
{
    public class UsuarioViewModel 
    {
        public string Nome { get; set; }        
        public string Login { get; set; }
        public string Email { get; set; } 
        public string Senha { get; set; }
        public string Celular { get; set; }
        public DateTime DataAtualizacao { get; set; }
    }    
}