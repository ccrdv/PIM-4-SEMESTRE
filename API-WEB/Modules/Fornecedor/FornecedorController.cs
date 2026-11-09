using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaWhiteLabel.API.Filters;

namespace SistemaWhiteLabel.API.Modules.Fornecedor
{
    [ApiController]
    [Route("api/[controller]")]
    [ModuloAtivo(FornecedorModule.NOME)]
    [Authorize(Roles = "Administrador,Gerente")]
    public class FornecedorController : ControllerBase
    {
        private readonly IFornecedorService _fornecedorService;

        public FornecedorController(IFornecedorService fornecedorService)
        {
            _fornecedorService = fornecedorService;
        }

        /// <summary>
        /// Lista os fornecedores com a quantidade e o valor das compras de cada um
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<FornecedorDto>>> GetAll()
        {
            return Ok(await _fornecedorService.ListarAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FornecedorDto>> GetById(int id)
        {
            var fornecedor = await _fornecedorService.GetByIdAsync(id);
            return fornecedor != null ? Ok(fornecedor) : NotFound(new { mensagem = "Fornecedor não encontrado." });
        }

        [HttpPost]
        public async Task<ActionResult<FornecedorDto>> Criar([FromBody] FornecedorSalvarDto dto)
        {
            try
            {
                var fornecedor = await _fornecedorService.CriarAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = fornecedor.IdFornecedor }, fornecedor);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<FornecedorDto>> Atualizar(int id, [FromBody] FornecedorSalvarDto dto)
        {
            try
            {
                var fornecedor = await _fornecedorService.AtualizarAsync(id, dto);
                return fornecedor != null ? Ok(fornecedor) : NotFound(new { mensagem = "Fornecedor não encontrado." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Excluir(int id)
        {
            try
            {
                return await _fornecedorService.ExcluirAsync(id)
                    ? NoContent()
                    : NotFound(new { mensagem = "Fornecedor não encontrado." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
