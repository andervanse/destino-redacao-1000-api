
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace destino_redacao_1000_api 
{
     [Route("api/[controller]")]
    public class PostagemController : RedacaoControllerBase
    {
        private readonly IPostagemRepository _repository;
        private readonly IUploadFile _uploadFile;
        private readonly ILogger<PostagemController> _logger;

        public PostagemController(
            IUploadFile uploadFile,
            IPostagemRepository repository,
            IConfiguration configuration, 
            ILogger<PostagemController> logger) : base(configuration, logger)
        {
            _repository = repository;
            _uploadFile = uploadFile;
            _logger = logger;
        }

        [HttpGet("{categoria}")]
        public async Task<IActionResult> Get(CategoriaPostagem categoria)
        {
            var response = await _repository.ObterPostagensAsync(categoria);

            if (response.HasError)
            {
                return BadRequest(response.ErrorMessages);
            }

            var lista = response
                .Return
                .Select(p => new PostagemViewModel {
                    Id = p.Id,
                    DataAtualizacao = p.DataAtualizacao,
                    Titulo = p.Titulo,
                    Texto = p.Texto,
                    UrlImagem = p.UrlImagem,
                    Categoria = p.Categoria
                }).ToList();

            return Ok(lista);
        }


        [HttpPost]        
        [Authorize(Policy = "Revisor")]
        public async Task<IActionResult> Post([FromForm] IFormCollection form)
        {
            if (form == null) 
               return BadRequest("Parâmetro nulo");

            int postagemId = 0;
            int.TryParse(form["id"], out postagemId);
            string titulo = form["titulo"];
            string texto = form["texto"];
            string urlImagem = form["urlImagem"];
            string categoria = form["categoria"];

            if (!ModelState.IsValid)
               return BadRequest(ModelState);
            
            var postagem         = new Postagem();
            postagem.Id          = postagemId;
            postagem.Titulo      = titulo;
            postagem.Texto       = texto;
            postagem.UrlImagem   = urlImagem;

            if (!String.IsNullOrEmpty(categoria))
            {
                Object cat = null;
                Enum.TryParse(typeof(CategoriaPostagem), categoria, true, out cat);

                if (cat != null)
                   postagem.Categoria = (CategoriaPostagem)cat;
            }

            var usuario          = this.ObterUsuario();
            postagem.Autor.Id    = usuario.Id;
            postagem.Autor.Email = usuario.Email;
            postagem.UrlImagem   = await SalvarImagemAsync(form);

            if (String.IsNullOrEmpty(postagem.UrlImagem) && form.Files.Count > 0)
            {
                return BadRequest("Falha ao realizar o upload da imagem.");
            }

            var response = await _repository.SalvarAsync(postagem);

            if (response.HasError)
            {
                return BadRequest(response.ErrorMessages);
            }

            return CreatedAtAction( actionName: nameof(Get), 
                                    routeValues: new { categoria = postagem.Categoria }, 
                                    value: response.Return);
        }   

        [HttpDelete("{id}")]
        [Authorize(Policy = "Revisor")]
        public async Task<IActionResult> Delete(int id, [FromBody] PostagemViewModel postagemVm)
        {
            if (postagemVm == null) 
               return BadRequest("Parâmetro nulo");

            if (postagemVm.Id <= 0)
               return BadRequest("Identificador inválido");

            var postagem = new Postagem();
            postagem.Id = postagemVm.Id;
            postagem.UrlImagem = postagemVm.UrlImagem;
            
            var usuario = ObterUsuario();

            if (!String.IsNullOrEmpty(postagem.UrlImagem))
            {
                Uri uri;
                
                if (!Uri.TryCreate(postagem.UrlImagem, UriKind.Absolute, out uri))
                    uri = new Uri(postagem.UrlImagem); 

                string fileName = Path.GetFileName(uri.LocalPath);
                await _uploadFile.DeleteFileAsync(usuario, fileName);
            }

            var response = await _repository.ExcluirAsync(postagem);

            if (response.HasError)
            {
                return BadRequest(response.ErrorMessages);
            }

            return Ok(response.Return);
        }    

        private async Task<string> SalvarImagemAsync(IFormCollection form)
        {
            long size = form.Files.Sum(f => f.Length);

            if (size > 0)
            {
                var formFile = form.Files[0];
                var urlLocation = "";

                if (formFile.Length > 0 && !String.IsNullOrEmpty(formFile.FileName))
                {
                    try
                    {
                        using (var mmStream = new MemoryStream())
                        {
                            var ext = Path.GetExtension(formFile.FileName);

                            if (ext.ToLower().IndexOf(".png") > -1
                                || ext.ToLower().IndexOf(".jpg") > -1 
                                || ext.ToLower().IndexOf(".jpeg") > -1) 
                            {
                                await formFile.CopyToAsync(mmStream); 
                                mmStream.Seek(0, SeekOrigin.Begin);
                                var usuario = ObterUsuario();
                                urlLocation = await _uploadFile.UploadFileAsync(usuario, mmStream, formFile.FileName);

                                if (String.IsNullOrEmpty(urlLocation))
                                {
                                    _logger.LogError("Upload", "Falha no Upload.");
                                }
                            }
                            else 
                            {
                                _logger.LogError("Upload", "Formato da imagem inválida.");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        var msgErro = "Falha ao processar imagem.";
                        _logger.LogError(e.Message, msgErro);
                    }
                }

                return urlLocation;
            }
            else
            {
                return null;
            }
        }     
    }
}