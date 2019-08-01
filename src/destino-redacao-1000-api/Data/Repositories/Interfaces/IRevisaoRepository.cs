using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace destino_redacao_1000_api
{
    public interface IRevisaoRepository
    {
        Task<Response<Revisao>> SalvarAsync(Revisao revisao);
        Task<Response<Revisao>> AtualizarRevisorAsync(Revisao revisao);
        Task<Response<List<Revisao>>> ObterNovasRevisoesAsync(Usuario usuario);
        Task<Response<List<Revisao>>> ObterRevisoesAssinanteAsync(Usuario usuario);
        Task<Response<List<Revisao>>> ObterRevisoesPendentesAsync(Usuario usuario);
        Task<Response<List<Revisao>>> ObterRevisoesFinalizadasAsync(Usuario usuario);
        Task<Response<Revisao>> DeletarAsync(Revisao revisao);
    }
}