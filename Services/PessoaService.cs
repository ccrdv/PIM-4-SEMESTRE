using Microsoft.EntityFrameworkCore;
using SistemaWhiteLabel.API.Data;
using SistemaWhiteLabel.API.DTOs;
using SistemaWhiteLabel.API.Models;
using SistemaWhiteLabel.API.Services.Interfaces;

namespace SistemaWhiteLabel.API.Services
{
    public class PessoaService : IPessoaService
    {
        private readonly ApplicationDbContext _context;

        public PessoaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PessoaResponseDto> CreateAsync(PessoaCreateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (dto.IsCliente && dto.IsFuncionario)
                    throw new Exception("Uma pessoa não pode ser cliente e funcionário ao mesmo tempo");

                var cpf = await ValidarPessoaAsync(dto, null);

                // 1. Criar a Pessoa
                var pessoa = new Pessoa
                {
                    Nome = dto.Nome.Trim(),
                    Cpf = cpf,
                    Telefone = dto.Telefone ?? string.Empty,
                    Observacao = string.IsNullOrWhiteSpace(dto.Observacao) ? null : dto.Observacao.Trim()
                };

                _context.Pessoas.Add(pessoa);
                await _context.SaveChangesAsync();   // Gera Id_Pessoa

                // 2. Criar o Endereço (se informado)
                if (!string.IsNullOrWhiteSpace(dto.Endereco))
                {
                    var endereco = new Endereco
                    {
                        IdPessoa = pessoa.IdPessoa,
                        Logradouro = dto.Endereco.Trim()
                    };

                    _context.Enderecos.Add(endereco);
                    await _context.SaveChangesAsync();
                }

                // 3. Criar o Funcionário (se for funcionário)
                // Cliente é a pessoa que não possui registro em Funcionario
                if (dto.IsFuncionario)
                {
                    var funcionario = new Funcionario
                    {
                        IdPessoa = pessoa.IdPessoa,
                        Cargo = dto.Cargo ?? "Vendedor",
                        Salario = dto.Salario ?? 0
                    };

                    _context.Funcionarios.Add(funcionario);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return new PessoaResponseDto
                {
                    IdPessoa = pessoa.IdPessoa,
                    Nome = pessoa.Nome,
                    Cpf = pessoa.Cpf,
                    Telefone = pessoa.Telefone ?? string.Empty,
                    Endereco = dto.Endereco ?? string.Empty,
                    IsCliente = !dto.IsFuncionario,
                    IsFuncionario = dto.IsFuncionario,
                    Observacao = pessoa.Observacao,
                    TotalFiado = 0,
                    Cargo = dto.IsFuncionario ? (dto.Cargo ?? "Vendedor") : null,
                    Salario = dto.IsFuncionario ? (dto.Salario ?? 0) : null
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // Erro de validação nosso: mensagem direta. Erro do banco: mostra o motivo dele.
                if (ex.InnerException == null)
                    throw;

                throw new Exception($"Erro ao criar pessoa: {ex.InnerException.Message}");
            }
        }

        public async Task<IEnumerable<PessoaResponseDto>> GetAllAsync()
        {
            var pessoas = await _context.Pessoas
                .Include(p => p.Enderecos)
                .Include(p => p.Funcionario)
                .Where(p => p.Funcionario == null)
                .ToListAsync();

            var compras = await ObterComprasAsync();

            return pessoas.Select(p =>
            {
                var endereco = ObterEndereco(p);

                return new PessoaResponseDto
                {
                    QuantidadeCompras = compras.ContainsKey(p.IdPessoa) ? compras[p.IdPessoa].Quantidade : 0,
                    TotalComprado = compras.ContainsKey(p.IdPessoa) ? compras[p.IdPessoa].Total : 0,
                    IdPessoa = p.IdPessoa,
                    Nome = p.Nome ?? string.Empty,
                    Cpf = p.Cpf ?? string.Empty,
                    Telefone = string.IsNullOrEmpty(p.Telefone) ? "Não informado" : p.Telefone,
                    Endereco = string.IsNullOrEmpty(endereco) ? "Não informado" : endereco,
                    IsCliente = p.Funcionario == null,
                    IsFuncionario = p.Funcionario != null,
                    Observacao = p.Observacao,
                    TotalFiado = 0,
                    Cargo = p.Funcionario != null ? p.Funcionario.Cargo : null,
                    Salario = p.Funcionario != null ? p.Funcionario.Salario : null
                };
            });
        }

        public async Task<PessoaResponseDto?> GetByIdAsync(int id)
        {
            var pessoa = await _context.Pessoas
                .Include(p => p.Enderecos)
                .Include(p => p.Funcionario)
                .FirstOrDefaultAsync(p => p.IdPessoa == id && p.Funcionario == null);

            if (pessoa == null)
                return null;

            var compras = await ObterComprasAsync();

            return new PessoaResponseDto
            {
                QuantidadeCompras = compras.ContainsKey(pessoa.IdPessoa) ? compras[pessoa.IdPessoa].Quantidade : 0,
                TotalComprado = compras.ContainsKey(pessoa.IdPessoa) ? compras[pessoa.IdPessoa].Total : 0,
                IdPessoa = pessoa.IdPessoa,
                Nome = pessoa.Nome,
                Cpf = pessoa.Cpf,
                Telefone = pessoa.Telefone ?? string.Empty,
                Endereco = ObterEndereco(pessoa),
                IsCliente = pessoa.Funcionario == null,
                IsFuncionario = pessoa.Funcionario != null,
                Observacao = pessoa.Observacao,
                TotalFiado = 0,
                Cargo = pessoa.Funcionario?.Cargo,
                Salario = pessoa.Funcionario?.Salario
            };
        }

        public async Task<PessoaResponseDto?> UpdateAsync(int id, PessoaCreateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var pessoa = await _context.Pessoas
                    .Include(p => p.Enderecos)
                    .Include(p => p.Funcionario)
                    .FirstOrDefaultAsync(p => p.IdPessoa == id);

                if (pessoa == null)
                    return null;

                var cpf = await ValidarPessoaAsync(dto, id);

                // Atualizar dados da pessoa
                pessoa.Nome = dto.Nome.Trim();
                pessoa.Cpf = cpf;
                pessoa.Telefone = dto.Telefone;
                pessoa.Observacao = string.IsNullOrWhiteSpace(dto.Observacao) ? null : dto.Observacao.Trim();

                // Atualizar endereço (o sistema trabalha com um único endereço por pessoa)
                var endereco = pessoa.Enderecos.OrderBy(e => e.IdEndereco).FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(dto.Endereco))
                {
                    if (endereco != null)
                    {
                        endereco.Logradouro = dto.Endereco.Trim();
                    }
                    else
                    {
                        _context.Enderecos.Add(new Endereco
                        {
                            IdPessoa = pessoa.IdPessoa,
                            Logradouro = dto.Endereco.Trim()
                        });
                    }
                }
                else if (endereco != null)
                {
                    // Endereço apagado no formulário
                    _context.Enderecos.Remove(endereco);
                }

                // Se for funcionário, atualizar cargo e salário
                if (pessoa.Funcionario != null)
                {
                    if (dto.Cargo != null)
                        pessoa.Funcionario.Cargo = dto.Cargo;
                    if (dto.Salario.HasValue)
                        pessoa.Funcionario.Salario = dto.Salario.Value;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new PessoaResponseDto
                {
                    IdPessoa = pessoa.IdPessoa,
                    Nome = pessoa.Nome,
                    Cpf = pessoa.Cpf,
                    Telefone = pessoa.Telefone ?? string.Empty,
                    Endereco = dto.Endereco ?? string.Empty,
                    IsCliente = pessoa.Funcionario == null,
                    IsFuncionario = pessoa.Funcionario != null,
                    Observacao = pessoa.Observacao,
                    TotalFiado = 0,
                    Cargo = pessoa.Funcionario?.Cargo,
                    Salario = pessoa.Funcionario?.Salario
                };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var pessoa = await _context.Pessoas
                    .Include(p => p.Enderecos)
                    .Include(p => p.Funcionario)
                    .FirstOrDefaultAsync(p => p.IdPessoa == id);

                if (pessoa == null)
                    return false;

                // O banco não apaga histórico em cascata: bloquear quem já possui vendas ou usuário
                if (await _context.Vendas.AnyAsync(v => v.IdPessoa == id))
                    throw new Exception("Não é possível excluir: a pessoa possui vendas registradas.");

                if (await _context.Usuarios.AnyAsync(u => u.IdPessoa == id))
                    throw new Exception("Não é possível excluir: a pessoa possui usuário de acesso.");

                // Remover endereços associados
                _context.Enderecos.RemoveRange(pessoa.Enderecos);

                // Remover funcionário associado
                if (pessoa.Funcionario != null)
                {
                    _context.Funcionarios.Remove(pessoa.Funcionario);
                }

                _context.Pessoas.Remove(pessoa);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<PessoaResponseDto>> BuscarAsync(string termo)
        {
            var pessoas = await _context.Pessoas
                .Include(p => p.Enderecos)
                .Include(p => p.Funcionario)
                .Where(p => p.Funcionario == null &&
                    (EF.Functions.Like(p.Nome, $"%{termo}%") ||
                     EF.Functions.Like(p.Cpf, $"%{termo}%")))
                .Take(20)
                .ToListAsync();

            var comprasBusca = await ObterComprasAsync();

            return pessoas.Select(p => new PessoaResponseDto
            {
                QuantidadeCompras = comprasBusca.ContainsKey(p.IdPessoa) ? comprasBusca[p.IdPessoa].Quantidade : 0,
                TotalComprado = comprasBusca.ContainsKey(p.IdPessoa) ? comprasBusca[p.IdPessoa].Total : 0,
                IdPessoa = p.IdPessoa,
                Nome = p.Nome,
                Cpf = p.Cpf,
                Telefone = p.Telefone ?? string.Empty,
                Endereco = ObterEndereco(p),
                IsCliente = p.Funcionario == null,
                IsFuncionario = p.Funcionario != null,
                Observacao = p.Observacao,
                TotalFiado = 0,
                Cargo = p.Funcionario?.Cargo,
                Salario = p.Funcionario?.Salario
            });
        }

        // Nome e CPF obrigatórios (CPF só com números, 11 dígitos e sem repetir). Devolve o CPF limpo.
        private async Task<string> ValidarPessoaAsync(PessoaCreateDto dto, int? idAtual)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome))
                throw new Exception("O nome é obrigatório.");

            var cpf = new string((dto.Cpf ?? string.Empty).Where(char.IsDigit).ToArray());

            if (cpf.Length != 11)
                throw new Exception("O CPF é obrigatório e precisa ter 11 números.");

            if (await _context.Pessoas.AnyAsync(p => p.Cpf == cpf && p.IdPessoa != (idAtual ?? 0)))
                throw new Exception("Já existe uma pessoa cadastrada com este CPF.");

            return cpf;
        }

        // Quantidade e total de compras de cada cliente (IdPessoa -> valores)
        private async Task<Dictionary<int, (int Quantidade, decimal Total)>> ObterComprasAsync()
        {
            var compras = await _context.Vendas
                .Where(v => v.IdPessoa != null)
                .GroupBy(v => v.IdPessoa)
                .Select(g => new { IdPessoa = g.Key, Quantidade = g.Count(), Total = g.Sum(v => v.TotalVenda) })
                .ToListAsync();

            return compras.ToDictionary(c => c.IdPessoa!.Value, c => (c.Quantidade, c.Total));
        }

        // Endereço de exibição (o sistema trabalha com um único endereço por pessoa)
        private static string ObterEndereco(Pessoa pessoa)
        {
            return pessoa.Enderecos
                .OrderBy(e => e.IdEndereco)
                .Select(e => e.Logradouro)
                .FirstOrDefault() ?? string.Empty;
        }
    }
}
