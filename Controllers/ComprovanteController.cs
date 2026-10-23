using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaWhiteLabel.API.Services;

namespace SistemaWhiteLabel.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ComprovanteController : ControllerBase
    {
        private readonly ComprovanteService _comprovanteService;

        public ComprovanteController(ComprovanteService comprovanteService)
        {
            _comprovanteService = comprovanteService;
        }

        [HttpGet("venda/{idVenda}")]
        public async Task<IActionResult> GerarComprovanteVenda(int idVenda)
        {
            try
            {
                var pdf = await _comprovanteService.GerarComprovanteVendaAsync(idVenda);
                return File(pdf, "application/pdf", $"comprovante_venda_{idVenda}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpGet("pagamento/{idVenda}")]
        public async Task<IActionResult> GerarComprovantePagamento(int idVenda, decimal valorPago, decimal saldoAnterior)
        {
            try
            {
                var pdf = await _comprovanteService.GerarComprovantePagamentoAsync(idVenda, valorPago, saldoAnterior);
                return File(pdf, "application/pdf", $"recibo_pagamento_venda_{idVenda}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}