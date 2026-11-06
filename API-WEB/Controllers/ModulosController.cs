using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaWhiteLabel.API.DTOs;
using SistemaWhiteLabel.API.Services.Interfaces;

namespace SistemaWhiteLabel.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModulosController : ControllerBase
    {
        private readonly IModuloService _moduloService;

        public ModulosController(IModuloService moduloService)
        {
            _moduloService = moduloService;
        }

        /// <summary>
        /// Lista os módulos e se estão ligados (o frontend usa para esconder menus)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ModuloDto>>> GetAll()
        {
            return Ok(await _moduloService.ListarAsync());
        }

        /// <summary>
        /// Liga ou desliga um módulo (só o perfil Administrador)
        /// </summary>
        [HttpPut("{nome}")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<ModuloDto>> Alterar(string nome, [FromBody] AlterarModuloDto dto)
        {
            var modulo = await _moduloService.AlterarAsync(nome, dto.Ativo);

            if (modulo == null)
                return NotFound(new { mensagem = "Módulo não encontrado" });

            return Ok(modulo);
        }
    }
}
