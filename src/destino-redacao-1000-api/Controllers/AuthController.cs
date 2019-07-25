using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace destino_redacao_1000_api
{
    [Authorize]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUsuarioRepository _userRepo;
        private readonly IConfiguration _config;
        private readonly IEmailLoginConfirmation _emailLoginConfirmation;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUsuarioRepository userRepo,
                              IConfiguration configuration,
                              IEmailLoginConfirmation emailLoginConfirmation,
                              ILogger<AuthController> logger)
        {
            _userRepo = userRepo;
            _config = configuration;
            _emailLoginConfirmation = emailLoginConfirmation;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Authentication([FromBody] CredenciaisUsuarioViewModel credenciais)
        {
            if (credenciais == null) return BadRequest();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = new Usuario
            {                
                Login = credenciais.Login,
                Senha = credenciais.Senha
            };

            var response = await _userRepo.UsuarioValidoAsync(user);

            if (response.HasError)
                return BadRequest(response.ErrorMessages);
            
            if (response.Return != null)
            {
                if (!response.Return.EmailConfirmado.Value)
                {
                    response.Messages.Add("E-mail não confirmado");
                    return BadRequest(response.Messages);
                }
                                
                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.UniqueName, response.Return.Login),
                    new Claim(JwtRegisteredClaimNames.NameId, response.Return.Id.ToString())
                };

                claims.Add(new Claim(ClaimTypes.Role, response.Return.TipoUsuario.ToString()));

                var token = new JwtSecurityToken(
                                issuer: _config["Token:Issuer"],
                                audience: _config["Token:Audience"],
                                claims: claims,
                                expires: DateTime.UtcNow.AddHours(6),
                                signingCredentials: new SigningCredentials(
                                                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Token:Key"])),
                                                SecurityAlgorithms.HmacSha256));

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.WriteToken(token);
                return Ok(new { 
                    token = jwtToken,
                    usuario = new UsuarioViewModel
                    {
                        Nome = response.Return.Nome,
                        Email = response.Return.Email,
                        TipoUsuario = response.Return.TipoUsuario,                      
                        DataAtualizacao = DateTime.Now
                    }
                });
            }
            else
            {
                return BadRequest();
            }
        }

        [AllowAnonymous]
        [HttpPost("email/{email}")]
        public async Task<IActionResult> ConfirmarEmailPost([FromBody] ConfirmaEmailViewModel confirmaEmail)
        {
            _logger.LogDebug("confirmar-email");
            if (confirmaEmail == null) return BadRequest();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var usuario = new Usuario 
            {
                Login = confirmaEmail.Email,
                Email = confirmaEmail.Email, 
                CodigoEmail = confirmaEmail.Codigo
            };

            var response = await _userRepo.ConfirmarEmailAsync(usuario);

            if (response.HasError) return BadRequest(response.ErrorMessages);

            if (response.Return == null) return NotFound();

            if (response.Return != null)
            {
                response.Return.EmailConfirmado = true;
                await _userRepo.SalvarAsync(response.Return);
            }

            return Ok();
        }

        [AllowAnonymous]
        [HttpPatch]
        public async Task<IActionResult> Patch([FromBody] LoginUsuarioViewModel loginUsuario)
        {
            if (loginUsuario == null) return BadRequest();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = new Usuario
            {
                Nome = loginUsuario.Login,
                Login = loginUsuario.Login,
                Senha = loginUsuario.Senha
            };

            var response = await _userRepo.ObterUsuarioAsync(user);

            if (response.HasError)
                return BadRequest(response.ErrorMessages);

            if (response.Return == null)
            {
                return BadRequest(response.ErrorMessages);
            }
            else
            {
                user.Id = response.Return.Id;

                var saveResponse = await _userRepo.SalvarAsync(user);

                if (saveResponse.HasError)
                    return BadRequest(saveResponse.ErrorMessages);
            }

            return Ok();
        }
    }
}