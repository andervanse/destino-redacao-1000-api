using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace destino_redacao_1000_api
{
    public interface IRevisaoRepository
    {
        Task<Response<Revisao>> SalvarAsync(Revisao revisao);
        Task<Response<List<Revisao>>> ObterListaAsync(Usuario usuario);
        Task<Response<Revisao>> ObterArquivoAsync(Usuario usuario, String urlArquivo);
    }
}