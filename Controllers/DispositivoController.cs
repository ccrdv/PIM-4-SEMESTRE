using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaWhiteLabel.API.DTOs;
using SistemaWhiteLabel.API.Helpers;
using SistemaWhiteLabel.API.Services.Interfaces;

namespace SistemaWhiteLabel.API.Controllers
{
    // Celulares autorizados pelo PC (QR Code).
    //   PC:      POST pareamento  ->  mostra o QR Code  ->  GET (lista) / renovar / DELETE
    //   Celular: POST parear (lê o QR Code, sem login)  ->  GET minha-sessao
    [ApiController]
    [Route("api/[controller]")]
    public class DispositivoController : ControllerBase
    {
        private readonly IDispositivoService _dispositivoService;

        public DispositivoController(IDispositivoService dispositivoService)
        {
            _dispositivoService = dispositivoService;
        }

        // Token de celular tem o claim "did". O celular não autoriza outros aparelhos nem mexe na lista:
        // isso é só do PC (sessão normal, com 2FA).
        private bool EhCelular => User.HasClaim(c => c.Type == "did");

        /// <summary>
        /// PC: gera o código do QR Code (vale 2 minutos, uma vez só)
        /// </summary>
        [HttpPost("pareamento")]
        public ActionResult<PareamentoDto> IniciarPareamento()
        {
            if (EhCelular)
                return Forbid();

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized(new { mensagem = "Usuário não identificado. Faça login novamente." });

            return Ok(_dispositivoService.IniciarPareamento(username));
        }

        /// <summary>
        /// Celular: troca o código lido no QR Code por uma sessão (não precisa de login)
        /// </summary>
        [HttpPost("parear")]
        [AllowAnonymous]
        public async Task<ActionResult<ParearResultadoDto>> Parear([FromBody] ParearDto dto)
        {
            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
                return Ok(await _dispositivoService.ParearAsync(dto.Codigo, dto.NomeDispositivo, ip));
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

        /// <summary>
        /// PC: lista os celulares conectados
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DispositivoResponseDto>>> GetAll()
        {
            if (EhCelular)
                return Forbid();

            return Ok(await _dispositivoService.ListarAsync(User.Identity?.Name ?? string.Empty));
        }

        /// <summary>
        /// PC: renova a sessão do celular por mais alguns dias (botão "Renovar")
        /// </summary>
        [HttpPost("{id}/renovar")]
        public async Task<ActionResult<DispositivoResponseDto>> Renovar(int id)
        {
            if (EhCelular)
                return Forbid();

            try
            {
                var result = await _dispositivoService.RenovarAsync(id, User.Identity?.Name ?? string.Empty);
                return result != null ? Ok(result) : NotFound(new { mensagem = "Aparelho não encontrado" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        /// <summary>
        /// PC: desconecta o celular na hora
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Desconectar(int id)
        {
            if (EhCelular)
                return Forbid();

            var ok = await _dispositivoService.RevogarAsync(id, User.Identity?.Name ?? string.Empty);
            return ok ? NoContent() : NotFound(new { mensagem = "Aparelho não encontrado" });
        }

        /// <summary>
        /// Celular: até quando a sessão dele vale
        /// </summary>
        [HttpGet("minha-sessao")]
        public async Task<ActionResult<MinhaSessaoDto>> MinhaSessao()
        {
            var did = User.FindFirst("did")?.Value;
            if (!int.TryParse(did, out var id))
                return BadRequest(new { mensagem = "Esta rota é só para o celular." });

            var result = await _dispositivoService.MinhaSessaoAsync(id);
            return result != null ? Ok(result) : NotFound();
        }
    }
}
