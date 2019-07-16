
using System;

namespace destino_redacao_1000_api
{
    public class Revisao
    {
        public Revisao()
        {
            Arquivo = new Arquivo();
        }

        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public Arquivo Arquivo { get; set; }
        public string Comentario { get; set; }
        public StatusRevisao StatusRevisao { get; set; }
        public DateTime DataPrevista { get; set; }
    }

    public class Arquivo 
    {
        public string Nome { get; set; }
        public string Url { get; set; }   
        public DateTime DataAtualizacao { get; set; }     
    }

    public enum StatusRevisao { NovaRevisao, EmRevisao, Revisado }
}