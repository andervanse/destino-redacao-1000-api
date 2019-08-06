namespace destino_redacao_1000_api
{
    public class DeletaRevisaoViewModel
    {
        public int Id { get; set; }        
        public StatusRevisao StatusRevisao { get; set; }
        public string ArquivoRef { get; set; }
        public Arquivo Arquivo { get; set; }
    }
}