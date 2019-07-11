using System;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace destino_redacao_1000_api
{
    public class Usuario 
    {
        public int Id { get; set; }
        public string Nome { get; set; }        
        public string Login { get; set; }
        public DateTime? DataAtualizacao { get; set; }
        public string Email { get; set; } 
        public string Senha { get; set; }
        public string Salt { get; set; }
        public string HashedPassword { get; set; }
        public string Celular { get; set; }   
        public string UrlFoto {get; set; }     
        public string Observacao { get; set; }
        public bool Administrador { get; set; }
    }
}