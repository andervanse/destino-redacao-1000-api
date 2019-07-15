
using System;

namespace destino_redacao_1000_api
{
    public class Arquivo 
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Nome { get; set; }
        public string Url { get; set; }   
        public DateTime DataAtualizacao { get; set; }     
    }
}