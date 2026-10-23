using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaWhiteLabel.API.DTOs;
using SistemaWhiteLabel.API.Services.Interfaces;

namespace SistemaWhiteLabel.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VendaController : ControllerBase
    {
        private readonly IVendaService _vendaService;

        public VendaController(IVendaService vendaService)
        {
            _vendaService = vendaService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<VendaResponseDto>> RegistrarVenda([FromBody] NovaVendaRequestDto dto)
        {
            try
            {
                // Usuário logado (o nome vem do token JWT)
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized(new { mensagem = "Usuário não identificado. Faça login novamente." });

                var venda = await _vendaService.RegistrarVendaAsync(dto, username);
                return CreatedAtAction(nameof(GetById), new { id = venda.IdVenda }, venda);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VendaResponseDto>>> GetAll()
        {
            var vendas = await _vendaService.GetAllAsync();
            return Ok(vendas);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VendaResponseDto>> GetById(int id)
        {
            var venda = await _vendaService.GetByIdAsync(id);
            return venda != null ? Ok(venda) : NotFound();
        }
    }
}
