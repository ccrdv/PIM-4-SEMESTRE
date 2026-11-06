using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaWhiteLabel.API.DTOs;
using SistemaWhiteLabel.API.Services.Interfaces;

namespace SistemaWhiteLabel.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador")]   // dados de funcionários (salário...): só o Administrador
    public class FuncionarioController : ControllerBase
    {
        private readonly IPessoaService _pessoaService;

        public FuncionarioController(IPessoaService pessoaService)
        {
            _pessoaService = pessoaService;
        }

        [HttpPost]
        public async Task<ActionResult<PessoaResponseDto>> Create([FromBody] PessoaCreateDto dto)
        {
            dto.IsCliente = false;
            dto.IsFuncionario = true;
            return await _pessoaService.CreateAsync(dto);
        }
    }
}