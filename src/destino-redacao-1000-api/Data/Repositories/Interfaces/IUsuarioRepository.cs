using System.Collections.Generic;
using System.Threading.Tasks;

namespace destino_redacao_1000_api
{
    public interface IUsuarioRepository
    {
        Task<Response<Usuario>> SalvarAsync(Usuario user);
        Task<Response<Usuario>> UsuarioValidoAsync(Usuario user);        
        Task<Response<List<Usuario>>> ObterUsuariosAsync(Usuario usuario);
        Task<Response<Usuario>> ObterUsuarioAsync(Usuario usuario);
    }
}