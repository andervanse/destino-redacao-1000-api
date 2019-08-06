using System;
using System.ComponentModel.DataAnnotations;

namespace destino_redacao_1000_api
{
    public class PostagemViewModel
    {
        
        public int Id { get; set; }
        
        [Required(ErrorMessage="Campo obrigatório")]
        public string Titulo { get; set; }

        [Required(ErrorMessage="Campo obrigatório")]
        public string Texto { get; set; }
        
        public string UrlImagem { get; set; }
        public DateTime DataAtualizacao { get; set; }
    }
}