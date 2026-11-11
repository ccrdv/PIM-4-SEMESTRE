using Microsoft.EntityFrameworkCore;
using SistemaWhiteLabel.API.Data;
using SistemaWhiteLabel.API.Helpers;
using SistemaWhiteLabel.API.Models;

namespace SistemaWhiteLabel.API.Modules.Fornecedor
{
    public class CompraService : ICompraService
    {
        public const string PENDENTE = "Pendente";
        public const string CONCLUIDO = "Concluido";
        public const string CANCELADO = "Cancelado";

        private readonly ApplicationDbContext _context;

        public CompraService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ====================== CONSULTAS ======================

        private IQueryable<Compra> ComprasCompletas()
        {
            return _context.Compras
                .Include(c => c.Fornecedor)
                .Include(c => c.StatusCompra)
                .Include(c => c.Itens).ThenInclude(i => i.Produto);
        }

        public async Task<List<CompraResponseDto>> ListarAsync(int? idFornecedor)
        {
            var query = ComprasCompletas().AsNoTracking();

            if (idFornecedor.HasValue)
                query = query.Where(c => c.IdFornecedor == idFornecedor.Value);

            var compras = await query.OrderByDescending(c => c.DataHora).ToListAsync();

            return compras.Select(ParaDto).ToList();
        }

        public async Task<CompraResponseDto?> GetByIdAsync(int id)
        {
            var compra = await ComprasCompletas().AsNoTracking().FirstOrDefaultAsync(c => c.IdCompra == id);
            return compra == null ? null : ParaDto(compra);
        }

        public async Task<ResumoComprasDto> GetResumoAsync()
        {
            var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            var pendentes = await _context.Compras
                .AsNoTracking()
                .Where(c => c.StatusCompra!.Descricao == PENDENTE)
                .Select(c => c.TotalCompra)
                .ToListAsync();

            var gastoMes = await _context.Compras
                .AsNoTracking()
                .Where(c => c.StatusCompra!.Descricao == CONCLUIDO && c.DataHora >= inicioMes)
                .Select(c => c.TotalCompra)
                .ToListAsync();

            return new ResumoComprasDto
            {
                FornecedoresAtivos = await _context.Fornecedores.CountAsync(f => f.Status == "Ativo"),
                ComprasPendentes = pendentes.Count,
                TotalPendente = pendentes.Sum(),
                GastoNoMes = gastoMes.Sum()
            };
        }

        private static CompraResponseDto ParaDto(Compra c)
        {
            return new CompraResponseDto
            {
                IdCompra = c.IdCompra,
                DataHora = c.DataHora,
                IdFornecedor = c.IdFornecedor,
                NomeFornecedor = c.Fornecedor?.Nome ?? string.Empty,
                Status = c.StatusCompra?.Descricao ?? string.Empty,
                TotalBruto = c.TotalCompra + c.TotalDesconto,
                TotalDesconto = c.TotalDesconto,
                TotalCompra = c.TotalCompra,
                Itens = c.Itens.Select(i => new ItemCompraResponseDto
                {
                    ProdutoId = i.IdProduto,
                    NomeProduto = i.Produto?.Descricao ?? string.Empty,
                    Quantidade = i.Qdte,
                    PrecoUnitario = i.PrecoUnitario,
                    ValorDesconto = i.ValorDesconto,
                    ValorTotal = i.ValorTotal
                }).ToList()
            };
        }

        // ====================== REGISTRAR ======================

        public async Task<CompraResponseDto> RegistrarAsync(NovaCompraDto dto, string username)
        {
            if (dto.Itens == null || !dto.Itens.Any())
                throw new Exception("A compra deve conter pelo menos um item.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Username == username);
                if (usuario == null)
                    throw new Exception("Usuário não encontrado.");

                var fornecedor = await _context.Fornecedores.FirstOrDefaultAsync(f => f.IdFornecedor == dto.IdFornecedor);
                if (fornecedor == null)
                    throw new Exception("Fornecedor não encontrado.");
                if (fornecedor.Status != "Ativo")
                    throw new Exception("Este fornecedor está inativo.");

                var produtoIds = dto.Itens.Select(i => i.ProdutoId).Distinct().ToList();
                var produtos = await _context.Produtos
                    .Where(p => produtoIds.Contains(p.IdProduto))
                    .ToDictionaryAsync(p => p.IdProduto);

                var compra = new Compra
                {
                    DataHora = DateTime.Now,
                    IdUsuario = usuario.IdUsuario,
                    IdFornecedor = fornecedor.IdFornecedor,
                    Itens = new List<ItemCompra>()
                };

                foreach (var itemDto in dto.Itens)
                {
                    if (!produtos.TryGetValue(itemDto.ProdutoId, out var produto))
                        throw new Exception($"Produto ID {itemDto.ProdutoId} não encontrado.");

                    if (itemDto.Quantidade <= 0)
                        throw new Exception($"A quantidade de {produto.Descricao} deve ser maior que zero.");

                    if (itemDto.PrecoUnitario <= 0)
                        throw new Exception($"Informe o custo unitário de {produto.Descricao}.");

                    var desconto = itemDto.Desconto ?? 0;
                    var valorItem = itemDto.PrecoUnitario * itemDto.Quantidade;

                    if (desconto < 0 || desconto > valorItem)
                        throw new Exception($"O desconto de {produto.Descricao} deve ficar entre zero e o valor do item.");

                    compra.Itens.Add(new ItemCompra
                    {
                        IdProduto = produto.IdProduto,
                        Qdte = itemDto.Quantidade,
                        PrecoUnitario = itemDto.PrecoUnitario,
                        ValorDesconto = desconto,
                        ValorTotal = valorItem - desconto
                    });
                }

                compra.TotalDesconto = compra.Itens.Sum(i => i.ValorDesconto);
                compra.TotalCompra = compra.Itens.Sum(i => i.ValorTotal);

                // Toda compra nasce Pendente; o recebimento (estoque, custo e status) é feito pela procedure do banco
                compra.IdStatusCompra = (await BuscarStatusAsync(PENDENTE)).IdStatusCompra;

                _context.Compras.Add(compra);
                await _context.SaveChangesAsync();

                if (dto.Receber)
                    await ProcedureHelper.ExecutarAsync(_context, ProcedureHelper.RECEBER_COMPRA, compra.IdCompra);

                await transaction.CommitAsync();

                return (await GetByIdAsync(compra.IdCompra))!;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ====================== RECEBER / CANCELAR ======================

        // A mercadoria chegou: a procedure do banco soma no estoque, atualiza o custo e a compra vira Concluido
        public async Task<CompraResponseDto> ReceberAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var compra = await _context.Compras
                    .AsNoTracking()
                    .Include(c => c.StatusCompra)
                    .FirstOrDefaultAsync(c => c.IdCompra == id);

                if (compra == null)
                    throw new KeyNotFoundException("Compra não encontrada.");

                if (compra.StatusCompra?.Descricao != PENDENTE)
                    throw new Exception("Só é possível receber uma compra Pendente.");

                await ProcedureHelper.ExecutarAsync(_context, ProcedureHelper.RECEBER_COMPRA, id);

                await transaction.CommitAsync();

                return (await GetByIdAsync(id))!;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Só cancela quem ainda é Pendente. Compra já recebida: acerte o estoque em Estoque > Ajustar
        public async Task<CompraResponseDto> CancelarAsync(int id)
        {
            var compra = await _context.Compras
                .Include(c => c.StatusCompra)
                .FirstOrDefaultAsync(c => c.IdCompra == id);

            if (compra == null)
                throw new KeyNotFoundException("Compra não encontrada.");

            if (compra.StatusCompra?.Descricao != PENDENTE)
                throw new Exception("Só é possível cancelar uma compra Pendente. Para uma compra já recebida, ajuste o estoque.");

            compra.StatusCompra = await BuscarStatusAsync(CANCELADO);

            await _context.SaveChangesAsync();

            return (await GetByIdAsync(id))!;
        }

        // ====================== AUXILIARES ======================

        private async Task<StatusCompra> BuscarStatusAsync(string descricao)
        {
            var status = await _context.StatusCompras.FirstOrDefaultAsync(s => s.Descricao == descricao);

            if (status == null)
                throw new Exception($"Status de compra '{descricao}' não cadastrado em tb_status_compra (rode o Banco/06_modulo_fornecedor.sql).");

            return status;
        }
    }
}
