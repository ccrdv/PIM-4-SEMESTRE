using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaWhiteLabel.API.Filters;

namespace SistemaWhiteLabel.API.Modules.Fornecedor
{
    [ApiController]
    [Route("api/[controller]")]
    [ModuloAtivo(FornecedorModule.NOME)]
    [Authorize(Roles = "Administrador,Gerente")]
    public class CompraController : ControllerBase
    {
        private readonly ICompraService _compraService;

        public CompraController(ICompraService compraService)
        {
            _compraService = compraService;
        }

        /// <summary>
        /// Lista as compras (mais novas primeiro). Com ?fornecedorId=1 só as desse fornecedor
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<CompraResponseDto>>> GetAll([FromQuery] int? fornecedorId)
        {
            return Ok(await _compraService.ListarAsync(fornecedorId));
        }

        [HttpGet("resumo")]
        public async Task<ActionResult<ResumoComprasDto>> GetResumo()
        {
            return Ok(await _compraService.GetResumoAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CompraResponseDto>> GetById(int id)
        {
            var compra = await _compraService.GetByIdAsync(id);
            return compra != null ? Ok(compra) : NotFound(new { mensagem = "Compra não encontrada." });
        }

        /// <summary>
        /// Registra uma compra. Receber = true já soma no estoque; false deixa a compra Pendente
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CompraResponseDto>> Registrar([FromBody] NovaCompraDto dto)
        {
            try
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized(new { mensagem = "Usuário não identificado. Faça login novamente." });

                var compra = await _compraService.RegistrarAsync(dto, username);
                return CreatedAtAction(nameof(GetById), new { id = compra.IdCompra }, compra);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        /// <summary>
        /// A mercadoria chegou: soma no estoque e conclui a compra
        /// </summary>
        [HttpPost("{id}/receber")]
        public async Task<ActionResult<CompraResponseDto>> Receber(int id)
        {
            try
            {
                return Ok(await _compraService.ReceberAsync(id));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPost("{id}/cancelar")]
        public async Task<ActionResult<CompraResponseDto>> Cancelar(int id)
        {
            try
            {
                return Ok(await _compraService.CancelarAsync(id));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensagem = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
