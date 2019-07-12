using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace destino_redacao_1000_api
{

    [Authorize]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class UserController :Controller
    {
        private readonly IUsuarioRepository _userRepo;
        private readonly IEmailLoginConfirmation _emailLoginConfirmation;
        private readonly IConfiguration _configuration;

        public UserController(IUsuarioRepository userRepo,
                              IEmailLoginConfirmation emailLoginConfirmation,
                              IConfiguration configuration)
        {
            _userRepo = userRepo;
            _emailLoginConfirmation = emailLoginConfirmation;
            _configuration = configuration;
        }

        [HttpGet("{id}", Name = "GetUser")]
        public async Task<IActionResult> Get(int id)
        {            
            if (id <= 0) return BadRequest();

            var response = await _userRepo.ObterUsuarioAsync(new Usuario {Id = id});
            return Ok(response);
        }

        [AllowAnonymous] 
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] LoginUsuarioViewModel loginUsuario)
        {
            if (loginUsuario == null) return BadRequest();

            if (!ModelState.IsValid) return BadRequest(ModelState);
            
            if (String.IsNullOrEmpty(loginUsuario.Nome)) {
                ModelState.AddModelError("Nome", "Campo obrigatório");
                return BadRequest(ModelState);
            }

            var user = new Usuario 
            {
                Login = loginUsuario.Login,
                Nome  = loginUsuario.Nome,
                Email = loginUsuario.Login,
                Senha = loginUsuario.Senha              
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

            if (String.IsNullOrEmpty(loginUsuario.Nome)) {
                ModelState.AddModelError("Nome", "Campo obrigatório");
                return BadRequest(ModelState);
            }
            
            var user = new Usuario 
            {
                Login = loginUsuario.Login,
                Nome  = loginUsuario.Nome,
                Email = loginUsuario.Login,
                Senha = loginUsuario.Senha       
            };

            var response = await _userRepo.ObterUsuarioAsync(user);

            if (response.HasError)
               return BadRequest(response.ErrorMessages);

            if (response.Return == null)
            {
                ModelState.AddModelError("Usuario", "Usuário não encontrado");
                return BadRequest(ModelState);
            }

            response = await _userRepo.SalvarAsync(user);

            if (response.HasError)            
               return BadRequest(response.ErrorMessages);
             
            return await EnviarEmailConfirmacaoAsync(response.Return, hasPasswordChanged: true);
        } 

        private async Task<CreatedAtRouteResult> EnviarEmailConfirmacaoAsync(Usuario usuario, bool hasPasswordChanged)
        {
            var emailConfirmationUrl = _configuration["Website:EmailConfirmationUrl"];
            var confirmationUrl = $"{emailConfirmationUrl}/{usuario.Email}/{usuario.CodigoEmailConfirmacao}";
            var threwExceptionSendingEmail = await _emailLoginConfirmation.SendAsync(usuario.Email, confirmationUrl, hasPasswordChanged);

            return CreatedAtRoute(routeName: "GetUser",
                                routeValues: new { id = usuario.Id },
                                      value: new { user = new { name = usuario.Nome, email = usuario.Email } });
        }                    
    }
}
