
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace destino_redacao_1000_api 
{
     [Route("api/[controller]")]
    public class PostagemController : RedacaoControllerBase
    {
        private readonly IPostagemRepository _repository;

        public PostagemController(
            IPostagemRepository repository,
            IConfiguration configuration, 
            ILogger<RedacaoControllerBase> logger) : base(configuration, logger)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var response = await _repository.ObterPostagensAsync();

            if (response.HasError)
            {
                return BadRequest(response.ErrorMessages);
            }

            return Ok(response.Return);
        }

        [HttpPost]
        [Authorize(policy: Policies.Revisor)]
        public async Task<IActionResult> Post(Postagem postagem)
        {
            if (postagem == null) 
               return BadRequest("Parâmetro nulo");

            if (!ModelState.IsValid)
               return BadRequest(ModelState);

            var usuario = this.ObterUsuario();
            postagem.Autor.Id = usuario.Id;
            postagem.Autor.Email = usuario.Email;
            var response = await _repository.SalvarAsync(postagem);

            if (response.HasError)
            {
                return BadRequest(response.ErrorMessages);
            }

            return Ok(response.Return);
        }   

        [HttpDelete("{id}")]
        [Authorize(Policies.Revisor)]
        public async Task<IActionResult> Delete(int id, [FromBody] Postagem postagem)
        {
            if (postagem == null) 
               return BadRequest("Parâmetro nulo");

            if (!ModelState.IsValid)
               return BadRequest(ModelState);

            var response = await _repository.ExcluirAsync(postagem);

            if (response.HasError)
            {
                return BadRequest(response.ErrorMessages);
            }

            return Ok(response.Return);
        }

    }

}