using Microsoft.EntityFrameworkCore;
using SistemaWhiteLabel.API.Data;

namespace SistemaWhiteLabel.API.Modules.Fornecedor
{
    public class FornecedorService : IFornecedorService
    {
        private readonly ApplicationDbContext _context;

        public FornecedorService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ====================== CONSULTAS ======================

        public async Task<List<FornecedorDto>> ListarAsync()
        {
            var fornecedores = await _context.Fornecedores
                .AsNoTracking()
                .OrderBy(f => f.Nome)
                .ToListAsync();

            var totais = await TotaisPorFornecedorAsync();

            return fornecedores.Select(f => ParaDto(f, totais)).ToList();
        }

        public async Task<FornecedorDto?> GetByIdAsync(int id)
        {
            var fornecedor = await _context.Fornecedores
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.IdFornecedor == id);

            if (fornecedor == null)
                return null;

            return ParaDto(fornecedor, await TotaisPorFornecedorAsync());
        }

        // Quantidade e valor das compras de cada fornecedor (as canceladas não contam)
        private async Task<Dictionary<int, (int Qtde, decimal Total)>> TotaisPorFornecedorAsync()
        {
            var linhas = await _context.Compras
                .AsNoTracking()
                .Where(c => c.StatusCompra!.Descricao != CompraService.CANCELADO)
                .GroupBy(c => c.IdFornecedor)
                .Select(g => new { Id = g.Key, Qtde = g.Count(), Total = g.Sum(c => c.TotalCompra) })
                .ToListAsync();

            return linhas.ToDictionary(x => x.Id, x => (x.Qtde, x.Total));
        }

        private static FornecedorDto ParaDto(Fornecedor f, Dictionary<int, (int Qtde, decimal Total)> totais)
        {
            totais.TryGetValue(f.IdFornecedor, out var t);

            return new FornecedorDto
            {
                IdFornecedor = f.IdFornecedor,
                Nome = f.Nome,
                Cnpj = f.Cnpj,
                Ramo = f.Ramo,
                Status = f.Status,
                Telefone = f.Telefone,
                Email = f.Email,
                QuantidadeCompras = t.Qtde,
                TotalComprado = t.Total
            };
        }

        // ====================== CADASTRO ======================

        public async Task<FornecedorDto> CriarAsync(FornecedorSalvarDto dto)
        {
            var fornecedor = new Fornecedor();
            await PreencherAsync(fornecedor, dto, null);

            _context.Fornecedores.Add(fornecedor);
            await _context.SaveChangesAsync();

            return (await GetByIdAsync(fornecedor.IdFornecedor))!;
        }

        public async Task<FornecedorDto?> AtualizarAsync(int id, FornecedorSalvarDto dto)
        {
            var fornecedor = await _context.Fornecedores.FirstOrDefaultAsync(f => f.IdFornecedor == id);

            if (fornecedor == null)
                return null;

            await PreencherAsync(fornecedor, dto, id);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        // Quem já tem compras não pode ser apagado (o histórico depende dele): inative o fornecedor
        public async Task<bool> ExcluirAsync(int id)
        {
            var fornecedor = await _context.Fornecedores.FirstOrDefaultAsync(f => f.IdFornecedor == id);

            if (fornecedor == null)
                return false;

            if (await _context.Compras.AnyAsync(c => c.IdFornecedor == id))
                throw new Exception("Este fornecedor tem compras registradas e não pode ser excluído. Marque-o como Inativo.");

            _context.Fornecedores.Remove(fornecedor);
            await _context.SaveChangesAsync();
            return true;
        }

        // Valida e copia os dados do formulário (idAtual = fornecedor que está sendo editado)
        private async Task PreencherAsync(Fornecedor fornecedor, FornecedorSalvarDto dto, int? idAtual)
        {
            var nome = (dto.Nome ?? string.Empty).Trim();
            if (nome.Length == 0)
                throw new Exception("Informe o nome do fornecedor.");
            if (nome.Length > 100)
                throw new Exception("O nome do fornecedor pode ter no máximo 100 caracteres.");

            var cnpj = new string((dto.Cnpj ?? string.Empty).Where(char.IsDigit).ToArray());
            if (cnpj.Length != 14)
                throw new Exception("O CNPJ deve ter 14 dígitos.");

            if (await _context.Fornecedores.AnyAsync(f => f.Cnpj == cnpj && f.IdFornecedor != (idAtual ?? 0)))
                throw new Exception("Já existe um fornecedor com este CNPJ.");

            var status = string.IsNullOrWhiteSpace(dto.Status) ? "Ativo" : dto.Status.Trim();
            if (status != "Ativo" && status != "Inativo")
                throw new Exception("O status deve ser Ativo ou Inativo.");

            var email = Limpar(dto.Email);
            if (email != null && !email.Contains('@'))
                throw new Exception("E-mail inválido.");

            fornecedor.Nome = nome;
            fornecedor.Cnpj = cnpj;
            fornecedor.Ramo = Limpar(dto.Ramo);
            fornecedor.Status = status;
            fornecedor.Telefone = Limpar(dto.Telefone);
            fornecedor.Email = email;
        }

        // Oracle grava texto vazio como NULL: já devolvemos null quando não há nada digitado
        private static string? Limpar(string? texto)
        {
            var t = (texto ?? string.Empty).Trim();
            return t.Length == 0 ? null : t;
        }
    }
}
