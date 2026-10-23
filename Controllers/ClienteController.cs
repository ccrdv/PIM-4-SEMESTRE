using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaWhiteLabel.API.DTOs;
using SistemaWhiteLabel.API.Services.Interfaces;
using SistemaWhiteLabel.API.Modules.Fiado;

namespace SistemaWhiteLabel.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClienteController : ControllerBase
    {
        private readonly IPessoaService _pessoaService;
        private readonly IFiadoGateway _fiado;

        public ClienteController(IPessoaService pessoaService, IFiadoGateway fiado)
        {
            _pessoaService = pessoaService;
            _fiado = fiado;
        }

        /// <summary>
        /// Criar novo cliente
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PessoaResponseDto>> Create([FromBody] PessoaCreateDto dto)
        {
            try
            {
                dto.IsCliente = true;
                dto.IsFuncionario = false;
                var result = await _pessoaService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.IdPessoa }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        /// <summary>
        /// Listar todos os clientes
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PessoaResponseDto>>> GetAll()
        {
            var clientes = (await _pessoaService.GetAllAsync()).ToList();

            // Saldo devedor de cada cliente (vem do módulo Fiado; vazio se estiver desligado)
            var saldos = await _fiado.SaldoPorClienteAsync();
            foreach (var cliente in clientes)
                cliente.TotalFiado = saldos.ContainsKey(cliente.IdPessoa) ? saldos[cliente.IdPessoa] : 0;

            return Ok(clientes);
        }

        /// <summary>
        /// Buscar cliente por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PessoaResponseDto>> GetById(int id)
        {
            var cliente = await _pessoaService.GetByIdAsync(id);
            if (cliente == null)
                return NotFound(new { mensagem = "Cliente não encontrado" });

            var saldos = await _fiado.SaldoPorClienteAsync();
            cliente.TotalFiado = saldos.ContainsKey(cliente.IdPessoa) ? saldos[cliente.IdPessoa] : 0;

            return Ok(cliente);
        }

        /// <summary>
        /// Atualizar dados do cliente
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador,Gerente")]
        public async Task<ActionResult<PessoaResponseDto>> Update(int id, [FromBody] PessoaCreateDto dto)
        {
            try
            {
                // Garantir que não está tentando converter cliente em funcionário
                dto.IsCliente = true;
                dto.IsFuncionario = false;

                var updated = await _pessoaService.UpdateAsync(id, dto);
                if (updated == null)
                    return NotFound(new { mensagem = "Cliente não encontrado" });

                return Ok(updated);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        /// <summary>
        /// Excluir cliente
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador,Gerente")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _pessoaService.DeleteAsync(id);
                if (!deleted)
                    return NotFound(new { mensagem = "Cliente não encontrado" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        /// <summary>
        /// Buscar clientes por nome ou CPF
        /// </summary>
        [HttpGet("buscar")]
        public async Task<ActionResult<IEnumerable<PessoaResponseDto>>> Buscar([FromQuery] string termo)
        {
            if (string.IsNullOrWhiteSpace(termo))
                return BadRequest(new { mensagem = "Termo de busca não informado" });

            var result = await _pessoaService.BuscarAsync(termo);
            return Ok(result);
        }
    }
}