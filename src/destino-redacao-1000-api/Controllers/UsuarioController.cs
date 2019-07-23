using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace destino_redacao_1000_api
{

    [Authorize]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class UsuarioController : Controller
    {
        private readonly IUsuarioRepository _userRepo;
        private readonly IEmailLoginConfirmation _emailLoginConfirmation;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UsuarioController> _logger;

        public UsuarioController(            
            IUsuarioRepository userRepo,
            IEmailLoginConfirmation emailLoginConfirmation,
            IConfiguration configuration,
            ILogger<UsuarioController> logger)
        {
            _userRepo = userRepo;
            _emailLoginConfirmation = emailLoginConfirmation;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("{id}", Name = "GetUser")]
        public async Task<IActionResult> Get(int id)
        {
            if (id <= 0) return BadRequest();

            var response = await _userRepo.ObterUsuarioAsync(new Usuario { Id = id });
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] LoginUsuarioViewModel loginUsuario)
        {
            if (loginUsuario == null) return BadRequest();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (String.IsNullOrEmpty(loginUsuario.Nome))
            {
                ModelState.AddModelError("Nome", "Campo obrigatório");
                return BadRequest(ModelState);
            }

            var user = new Usuario
            {
                Login = loginUsuario.Login.ToLower(),
                Nome = loginUsuario.Nome,
                Email = loginUsuario.Login.ToLower(),
                Senha = loginUsuario.Senha,
                TipoUsuario = TipoUsuario.Assinante
            };

            var response = await _userRepo.ObterUsuarioAsync(user);

            if (response.HasError)
                return BadRequest(response.ErrorMessages);

            if (response.Return == null)
            {
                response = await _userRepo.SalvarAsync(user);

                if (response.HasError)
                    return BadRequest(response.ErrorMessages);
            }
            else
            {
                ModelState.AddModelError("Login", $"O login '{response.Return.Email}' já existe.");
                return BadRequest(ModelState);
            }

            return await EnviarEmailConfirmacaoAsync(response.Return, hasPasswordChanged: false);
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] LoginUsuarioViewModel loginUsuario)
        {
            if (loginUsuario == null) return BadRequest();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (String.IsNullOrEmpty(loginUsuario.Nome))
            {
                ModelState.AddModelError("Nome", "Campo obrigatório");
                return BadRequest(ModelState);
            }

            var usuario = new Usuario
            {
                Login = loginUsuario.Login.ToLower(),
                Nome = loginUsuario.Nome,
                Email = loginUsuario.Login.ToLower(),
                Senha = loginUsuario.Senha,
                TipoUsuario = TipoUsuario.Assinante
            };

            var response = await _userRepo.ObterUsuarioAsync(usuario);

            if (response.HasError)
                return BadRequest(response.ErrorMessages);

            if (response.Return == null)
            {
                ModelState.AddModelError("Usuario", "Usuário não encontrado");
                return BadRequest(ModelState);
            }

            usuario.Id = response.Return.Id;
            usuario.Email = response.Return.Email;
            response = await _userRepo.SalvarAsync(usuario);

            if (response.HasError)
                return BadRequest(response.ErrorMessages);

            return await EnviarEmailConfirmacaoAsync(response.Return, hasPasswordChanged: true);
        }

        [AllowAnonymous]
        [HttpPatch("Senha")]
        public async Task<IActionResult> ResetarSenhaPatch([FromBody] RedefinicaoSenhaViewModel redefinicaoSenha)
        {
            if (redefinicaoSenha == null) return BadRequest();

            var user = new Usuario
            {
                Login = redefinicaoSenha.Email.ToLower(),
                Email = redefinicaoSenha.Email.ToLower(),
                CodigoEmail = String.Empty
            };

            var response = await _userRepo.ResetarSenhaAsync(user);

            if (response.HasError || response.Return == null)
            {
                return BadRequest(response.ErrorMessages);
            }
            else
            {
                return await EnviarEmailResetarSenhaAsync(response.Return, hasPasswordChanged: true);
            }
        }

        [AllowAnonymous]
        [HttpPut("Senha")]
        public async Task<IActionResult> NovaSenhaPut([FromBody] LoginUsuarioViewModel loginUsuario)
        {
            if (loginUsuario == null) return BadRequest();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var usuario = new Usuario
            {
                Login = loginUsuario.Login,
                Email = loginUsuario.Login,
                Senha = loginUsuario.Senha,
                CodigoEmail = loginUsuario.CodigoResetSenha
            };

            var response = await _userRepo.ResetarSenhaAsync(usuario);

            if (response.HasError)
                return BadRequest(response.ErrorMessages);
            else
                return Ok(new { Nome = response.Return.Nome, Login = response.Return.Login });
        }

        private async Task<IActionResult> EnviarEmailResetarSenhaAsync(Usuario usuario, bool hasPasswordChanged)
        {
            var emailResetPasswordUrl = _configuration["Website:EmailResetPasswordUrl"];
            var confirmationUrl = $"{emailResetPasswordUrl}/{usuario.Email}/{usuario.CodigoResetSenha}";
            var threwExceptionSendingEmail = await _emailLoginConfirmation.SendAsync(usuario.Email, confirmationUrl, hasPasswordChanged);

            if (threwExceptionSendingEmail)
               return StatusCode(500, new { message = "Falha ao enviar email"});
            else   
               return CreatedAtRoute(routeName: "GetUser",
                                   routeValues: new { id = usuario.Id },
                                         value: new { name = usuario.Nome, email = usuario.Email });
        }

        private async Task<IActionResult> EnviarEmailConfirmacaoAsync(Usuario usuario, bool hasPasswordChanged)
        {
            var emailConfirmationUrl = _configuration["Website:EmailConfirmationUrl"];
            var confirmationUrl = $"{emailConfirmationUrl}/{usuario.Email}/{usuario.CodigoEmail}";
            var threwExceptionSendingEmail = await _emailLoginConfirmation.SendAsync(usuario.Email, confirmationUrl, hasPasswordChanged);

            if (threwExceptionSendingEmail)
               return StatusCode(500, new { message = "Falha ao enviar email"});
            else   
               return CreatedAtRoute(routeName: "GetUser",
                                   routeValues: new { id = usuario.Id },
                                         value: new { name = usuario.Nome, email = usuario.Email });
        }
    }
}
