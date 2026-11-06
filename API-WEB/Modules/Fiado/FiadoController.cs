using Microsoft.AspNetCore.Mvc;
using SistemaWhiteLabel.API.Filters;

namespace SistemaWhiteLabel.API.Modules.Fiado
{
    [ApiController]
    [Route("api/[controller]")]
    [ModuloAtivo(FiadoModule.NOME)]
    public class FiadoController : ControllerBase
    {
        private readonly IFiadoService _fiadoService;

        public FiadoController(IFiadoService fiadoService)
        {
            _fiadoService = fiadoService;
        }

        /// <summary>
        /// Retorna resumo completo dos fiados
        /// </summary>
        [HttpGet("resumo")]
        public async Task<ActionResult<TotalFiadosDto>> GetResumoFiados()
        {
            return Ok(await _fiadoService.GetResumoAsync());
        }

        /// <summary>
        /// Lista todas as vendas fiadas com saldo pendente
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<FiadoResponseDto>>> GetAllFiados()
        {
            return Ok(await _fiadoService.GetPendentesAsync());
        }

        /// <summary>
        /// Registra pagamento parcial ou total de um fiado
        /// </summary>
        [HttpPost("pagar")]
        public async Task<ActionResult<PagamentoResponseDto>> RegistrarPagamento([FromBody] PagamentoFiadoDto dto)
        {
            try
            {
                var result = await _fiadoService.RegistrarPagamentoAsync(dto);
                return Ok(result);
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

        [HttpGet("historico/{clienteId}")]
        public async Task<ActionResult<HistoricoFiadoClienteDto>> GetHistoricoCliente(int clienteId)
        {
            var historico = await _fiadoService.GetHistoricoClienteAsync(clienteId);

            if (historico == null)
                return NotFound(new { mensagem = "Cliente não encontrado" });

            return Ok(historico);
        }
    }
}
