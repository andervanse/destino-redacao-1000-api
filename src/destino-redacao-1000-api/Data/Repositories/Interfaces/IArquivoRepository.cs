using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace destino_redacao_1000_api
{
    public interface IArquivoRepository
    {
        Task<Response<Arquivo>> SalvarAsync(Arquivo arquivo);
        Task<Response<List<Arquivo>>> ObterListaAsync(Usuario usuario);
        Task<Response<Arquivo>> ObterArquivoAsync(Usuario usuario, String urlArquivo);
    }
}