using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaWhiteLabel.API.DTOs;
using SistemaWhiteLabel.API.Helpers;
using SistemaWhiteLabel.API.Services.Interfaces;

namespace SistemaWhiteLabel.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // ETAPA 1: usuário e senha -> devolve o token temporário (bearer da etapa do 2FA)
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginEtapa1Dto>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var result = await _authService.LoginAsync(loginDto);

                if (result == null)
                    return Unauthorized(new { mensagem = "Usuário ou senha inválidos" });

                return Ok(result);
            }
            catch (TentativasExcedidasException ex)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new { mensagem = ex.Message });
            }
        }

        // ETAPA 2 (primeiro acesso): gera a chave/QR Code para o app autenticador
        // Exige o token temporário no header: Authorization: Bearer {tokenTemporario}
        [HttpPost("2fa/configurar")]
        [Authorize(Policy = "Etapa2FA")]
        public async Task<ActionResult<ConfigurarDoisFatoresDto>> ConfigurarDoisFatores()
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized(new { mensagem = "Usuário não identificado. Faça login novamente." });

                var result = await _authService.ConfigurarDoisFatoresAsync(username);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // ETAPA 2 (primeiro acesso): confirma o primeiro código e abre a sessão
        [HttpPost("2fa/ativar")]
        [Authorize(Policy = "Etapa2FA")]
        public async Task<ActionResult<TokenDto>> AtivarDoisFatores([FromBody] AtivarDoisFatoresDto dto)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized(new { mensagem = "Usuário não identificado. Faça login novamente." });

                var result = await _authService.AtivarDoisFatoresAsync(username, dto);

                if (result == null)
                    return Unauthorized(new { mensagem = "Código inválido ou expirado" });

                return Ok(result);
            }
            catch (TentativasExcedidasException ex)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new { mensagem = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // ETAPA 2 (acessos seguintes): confere o código do app e abre a sessão
        [HttpPost("2fa/verificar")]
        [Authorize(Policy = "Etapa2FA")]
        public async Task<ActionResult<TokenDto>> VerificarDoisFatores([FromBody] VerificarDoisFatoresDto dto)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized(new { mensagem = "Usuário não identificado. Faça login novamente." });

                var result = await _authService.VerificarDoisFatoresAsync(username, dto.Codigo);

                if (result == null)
                    return Unauthorized(new { mensagem = "Código inválido ou expirado" });

                return Ok(result);
            }
            catch (TentativasExcedidasException ex)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new { mensagem = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        // A sessão não fica guardada no servidor: o logout é feito pelo
        // navegador (apaga o token). O token deixa de valer sozinho quando o tempo acaba.
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok(new { mensagem = "Logout realizado com sucesso" });
        }

        [HttpGet("check-token")]
        public IActionResult CheckToken()
        {
            var username = User.Identity?.Name;
            var exp = User.FindFirst("exp")?.Value;

            DateTime? expiraEm = null;
            if (long.TryParse(exp, out var expSegundos))
                expiraEm = DateTimeOffset.FromUnixTimeSeconds(expSegundos).UtcDateTime;

            return Ok(new
            {
                mensagem = "Token válido",
                username = username,
                expiraEm = expiraEm
            });
        }
    }
}
