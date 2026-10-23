using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaWhiteLabel.API.DTOs;
using SistemaWhiteLabel.API.Models;
using SistemaWhiteLabel.API.Data;
using Microsoft.EntityFrameworkCore;

namespace SistemaWhiteLabel.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador")]   // dados de funcionários (salário...): só o Administrador
    public class PessoaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PessoaController(ApplicationDbContext context)
        {
            _context = context;
        }


        /// Cadastra uma nova pessoa (Cliente ou Funcionário)
        [HttpPost]
        public async Task<ActionResult<PessoaResponseDto>> Create([FromBody] PessoaCreateDto dto)
        {
            var pessoa = new Pessoa
            {
                Nome = dto.Nome,
                Cpf = dto.Cpf,
                Telefone = dto.Telefone
            };

            _context.Pessoas.Add(pessoa);
            await _context.SaveChangesAsync();

            // Criar endereço (se informado)
            if (!string.IsNullOrWhiteSpace(dto.Endereco))
            {
                _context.Enderecos.Add(new Endereco
                {
                    IdPessoa = pessoa.IdPessoa,
                    Logradouro = dto.Endereco.Trim()
                });
            }

            // Criar como Funcionário (cliente é a pessoa sem registro em Funcionario)
            if (dto.IsFuncionario)
            {
                var funcionario = new Funcionario
                {
                    IdPessoa = pessoa.IdPessoa,
                    Cargo = dto.Cargo ?? "Vendedor",
                    Salario = dto.Salario ?? 0
                };
                _context.Funcionarios.Add(funcionario);
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = pessoa.IdPessoa }, new PessoaResponseDto
            {
                IdPessoa = pessoa.IdPessoa,
                Nome = pessoa.Nome,
                Cpf = pessoa.Cpf,
                Telefone = pessoa.Telefone ?? string.Empty,
                IsCliente = !dto.IsFuncionario,
                IsFuncionario = dto.IsFuncionario
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PessoaResponseDto>> GetById(int id)
        {
            var pessoa = await _context.Pessoas.FindAsync(id);
            if (pessoa == null) return NotFound("Pessoa não encontrada.");

            var isFuncionario = await _context.Funcionarios.AnyAsync(f => f.IdPessoa == id);

            return Ok(new PessoaResponseDto
            {
                IdPessoa = pessoa.IdPessoa,
                Nome = pessoa.Nome,
                Cpf = pessoa.Cpf,
                Telefone = pessoa.Telefone ?? string.Empty,
                IsCliente = !isFuncionario,
                IsFuncionario = isFuncionario
            });
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PessoaResponseDto>>> GetAll()
        {
            var pessoas = await _context.Pessoas.ToListAsync();
            var idsFuncionarios = await _context.Funcionarios.Select(f => f.IdPessoa).ToListAsync();
            var result = new List<PessoaResponseDto>();

            foreach (var p in pessoas)
            {
                var isFuncionario = idsFuncionarios.Contains(p.IdPessoa);

                result.Add(new PessoaResponseDto
                {
                    IdPessoa = p.IdPessoa,
                    Nome = p.Nome,
                    Cpf = p.Cpf,
                    Telefone = p.Telefone ?? string.Empty,
                    IsCliente = !isFuncionario,
                    IsFuncionario = isFuncionario
                });
            }

            return Ok(result);
        }
    }
}
