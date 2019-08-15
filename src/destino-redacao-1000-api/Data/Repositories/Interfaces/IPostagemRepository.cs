using System.Collections.Generic;
using System.Threading.Tasks;

namespace destino_redacao_1000_api
{
    public interface IPostagemRepository
    {
        Task<Response<IEnumerable<Postagem>>> ObterPostagensAsync(CategoriaPostagem categoria);
        Task<Response<Postagem>> SalvarAsync(Postagem postagem);
        Task<Response<Postagem>> ExcluirAsync(Postagem postagem);
    }  
}