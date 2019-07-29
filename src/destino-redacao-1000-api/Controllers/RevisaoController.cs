using System;
using System.Collections.Generic;
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
    [Authorize]
    [Route("api/[controller]")]
    public class RevisaoController : RedacaoControllerBase
    {
        private readonly IUploadFile _uploadFile;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IRevisaoRepository _revisaoRepository;
        private readonly ILogger _logger;

        public RevisaoController(
            IUsuarioRepository usuarioRepository,
            IRevisaoRepository revisaoRepository,
            IUploadFile uploadFile,
            IConfiguration configuration,
            ILogger<RevisaoController> logger) : base(configuration, logger)
        {
            _usuarioRepository = usuarioRepository;
            _revisaoRepository = revisaoRepository;
            _uploadFile = uploadFile;            
            _logger = logger;
        }

        [HttpGet("pendentes")]
        public async Task<ActionResult> GetRevisoesPendentes()
        {
            var usuario = ObterUsuario();
            var response = await _revisaoRepository.ObterRevisoesPendentesAsync(usuario);

            if (response.HasError)
            {
                _logger.LogError("revisao/pendentes", response.ErrorMessages);
                return BadRequest(response.ErrorMessages);                
            }

            return Ok(response.Return);
        }

        [HttpGet("novas")]
        public async Task<ActionResult> GetNovasRevisoes()
        {
            var usuario = ObterUsuario();

            if (usuario.TipoUsuario == TipoUsuario.Assinante)
               return BadRequest(new { message = "Usuário inválido." });

            var response = await _revisaoRepository.ObterNovasRevisoesAsync(usuario);

            if (response.HasError)
            {
                _logger.LogError("NovasRevisoes", response.ErrorMessages);
                return BadRequest(response.ErrorMessages);                
            }

            return Ok(response.Return);
        }  

        [HttpGet]
        public async Task<ActionResult> GetRevisoesAssinante()
        {
            var assinante = ObterUsuario();

            if (assinante.TipoUsuario != TipoUsuario.Assinante)
               return BadRequest(new { message = "Usuário inválido." });

            var response = await _revisaoRepository.ObterRevisoesAssinanteAsync(assinante);

            if (response.HasError)
            {
                _logger.LogError("Revisões Assinante", response.ErrorMessages);
                return BadRequest(response.ErrorMessages);                
            }

            return Ok(response.Return);
        }          

        [HttpPatch]
        public async Task<ActionResult> Patch([FromBody] AtualizaNovaRevisaoViewModel atualizaRevisao)
        {
            var revisor = ObterUsuario();

            if (revisor.TipoUsuario == TipoUsuario.Assinante)
               return BadRequest(new { message = "Usuário inválido." });
            
            var revisao = new Revisao {
                Id = atualizaRevisao.RevisaoId,
                AssinanteId = atualizaRevisao.AssinanteId,
                RevisorId = revisor.Id                
            };

            var response = await _revisaoRepository.AtualizarRevisorAsync(revisao);

            if (response.HasError)
            {
                _logger.LogError("Lista", response.ErrorMessages);
                return BadRequest(response.ErrorMessages);                
            }

            return Ok();
        }               

        [HttpPost]
        public Task<IActionResult> Post([FromForm] IFormCollection form)
        {
            return UploadFile(form, TipoArquivo.Revisao);
        }

        [HttpPost("correcao")]
        public Task<IActionResult> CorrecaoPost([FromForm] IFormCollection form)
        {
            return UploadFile(form, TipoArquivo.Correcao);
        }

        [HttpDelete("{keyName}")]
        public async Task<IActionResult> Delete(string keyName)
        {
            try
            {
                var usuario = ObterUsuario();
                await _uploadFile.DeleteFileAsync(usuario, keyName);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return BadRequest(e.Message);
            }

            return Ok();
        }


        private async Task<IActionResult> UploadFile(IFormCollection form, TipoArquivo tpArquivo)
        {
            long size = form.Files.Sum(f => f.Length);
            string comentario = form["comentario"];
            string tipoArquivo = form["tipoArquivo"];
            int revisaoIdRef = 0;
            int.TryParse(form["revisaoIdRef"], out revisaoIdRef);
            int revisaoId = 0;
            int.TryParse(form["revisaoId"], out revisaoId);
            int assinanteId = 0;
            int.TryParse(form["assinanteId"], out assinanteId);

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

                            if (ext.IndexOf(".docx") > -1 || ext.IndexOf(".doc") > -1) 
                            {
                                await formFile.CopyToAsync(mmStream); 
                                mmStream.Seek(0, SeekOrigin.Begin);
                                var usuario = ObterUsuario();
                                urlLocation = await _uploadFile.UploadFileAsync(usuario, mmStream, formFile.FileName);

                                if (!String.IsNullOrEmpty(urlLocation))
                                {
                                    var revisao = new Revisao
                                    {
                                        Id = revisaoId,
                                        AssinanteId = assinanteId,
                                        AssinanteEmail = usuario.Email,
                                        RevisaoIdRef = revisaoIdRef,
                                        Arquivo = new Arquivo
                                        {
                                            Nome = formFile.FileName,
                                            Url = urlLocation,
                                            TipoArquivo = tpArquivo,
                                            DataAtualizacao = DateTime.Now
                                        },
                                        Comentario = comentario
                                    };

                                    if (usuario.TipoUsuario == TipoUsuario.Revisor)
                                    {
                                      revisao.RevisorId = usuario.Id;
                                      revisao.StatusRevisao = StatusRevisao.Revisado;
                                    }
                                    else
                                    {
                                      revisao.AssinanteId = usuario.Id;
                                    }

                                    var response = await _revisaoRepository.SalvarAsync(revisao);

                                    if (response.HasError)
                                    {
                                       _logger.LogError("Upload", response.ErrorMessages);
                                       return BadRequest(response.ErrorMessages);
                                    }
                                }
                                else
                                {
                                    _logger.LogError("Upload", "Falha no Upload.");
                                    return BadRequest(new { message = "Falha no Upload." });
                                }
                            }
                            else 
                            {
                                _logger.LogError("Upload", "Extensão do arquivo inválida.");
                                return BadRequest(new { message = "Extensão do arquivo inválida." });
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        var msgErro = "Falha ao processar arquivo.";
                        _logger.LogError(e.Message, msgErro);
                        return StatusCode(500, new { message = msgErro });
                    }
                }

                return Created(urlLocation, new { message = "Created" });
            }
            else
            {
                return BadRequest("Arquivo inválido.");
            }
        }
    }
}