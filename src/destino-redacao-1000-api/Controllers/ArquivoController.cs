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
    public class ArquivoController : RedacaoControllerBase
    {
        private readonly IUploadFile _uploadFile;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IArquivoRepository _arquivoRepository;
        private readonly ILogger _logger;

        public ArquivoController(
            IUsuarioRepository usuarioRepository,
            IArquivoRepository arquivoRepository,
            IUploadFile uploadFile,
            IConfiguration configuration,
            ILogger<ArquivoController> logger) : base(configuration, logger)
        {
            _usuarioRepository = usuarioRepository;
            _arquivoRepository = arquivoRepository;
            _uploadFile = uploadFile;            
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromForm] IFormCollection form)
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

                            if (ext.IndexOf(".docx") > -1 || ext.IndexOf(".doc") > -1) 
                            {
                                var fileId = $"{Guid.NewGuid()}{ext}";
                                await formFile.CopyToAsync(mmStream); 
                                mmStream.Seek(0, SeekOrigin.Begin);
                                var usuario = ObterUsuario();
                                urlLocation = await _uploadFile.UploadFileAsync(usuario, mmStream, $"{fileId}");

                                if (!String.IsNullOrEmpty(urlLocation))
                                {
                                    var arquivo = new Arquivo
                                    {
                                        Nome = formFile.FileName,
                                        UsuarioId = usuario.Id,
                                        Url = urlLocation
                                    };

                                    var response = await _arquivoRepository.SalvarAsync(arquivo);

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

                return Ok(new { urlLocation = urlLocation });
            }
            else
            {
                return BadRequest("Empty file");
            }
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
    }
}