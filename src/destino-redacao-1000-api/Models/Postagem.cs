using System;

namespace destino_redacao_1000_api
{
    public class Postagem
    {
        public Postagem()
        {
            this.Autor = new Usuario();
        }

        public int Id { get; set; }
        public Usuario Autor { get; set; }
        public string Titulo { get; set; }
        public string Texto { get; set; }
        public string UrlImagem { get; set; }
        public CategoriaPostagem Categoria { get; set; }
        public DateTime DataAtualizacao { get; set; }        
    }

    public enum CategoriaPostagem { Indefinida, Redacoes, Dicas };
}