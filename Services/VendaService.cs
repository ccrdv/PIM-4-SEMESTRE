using SistemaWhiteLabel.API.DTOs;
using SistemaWhiteLabel.API.Models;
using SistemaWhiteLabel.API.Repositories.Interfaces;
using SistemaWhiteLabel.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using SistemaWhiteLabel.API.Data;
using SistemaWhiteLabel.API.Modules.Fiado;
using SistemaWhiteLabel.API.Helpers;

namespace SistemaWhiteLabel.API.Services
{
    public class VendaService : IVendaService
    {
        private readonly IVendaRepository _vendaRepository;
        private readonly IProdutoRepository _produtoRepository;
        private readonly ApplicationDbContext _context;
        private readonly IFiadoGateway _fiado;

        public VendaService(
            IVendaRepository vendaRepository,
            IProdutoRepository produtoRepository,
            ApplicationDbContext context,
            IFiadoGateway fiado)
        {
            _vendaRepository = vendaRepository;
            _produtoRepository = produtoRepository;
            _context = context;
            _fiado = fiado;
        }

        public async Task<VendaResponseDto> RegistrarVendaAsync(NovaVendaRequestDto dto, string username)
        {
            if (dto.Itens == null || !dto.Itens.Any())
                throw new Exception("A venda deve conter pelo menos um item.");

            // Venda fiada só existe com o módulo de fiado ligado e com um cliente
            if (dto.IsFiado)
            {
                if (!await _fiado.EstaAtivoAsync())
                    throw new Exception("Venda fiada indisponível: o módulo de fiado está desativado.");

                if (!dto.ClienteId.HasValue)
                    throw new Exception("Selecione o cliente para a venda fiada.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Usuário que está registrando a venda (vem do token)
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (usuario == null)
                    throw new Exception("Usuário não encontrado.");

                // Forma de pagamento escolhida (tbl_Tipo_Pagamento)
                // Na venda fiada ninguém pagou ainda: não existe forma de pagamento nem pagamento
                TipoPagamento? tipoPagamento = null;

                if (!dto.IsFiado)
                {
                    var formaPagamento = (dto.FormaPagamento ?? string.Empty).Trim().ToUpper();
                    tipoPagamento = await _context.TiposPagamento
                        .FirstOrDefaultAsync(t => t.Descricao.ToUpper() == formaPagamento);

                    if (tipoPagamento == null)
                        throw new Exception($"Forma de pagamento '{dto.FormaPagamento}' não cadastrada.");
                }

                var venda = new Venda
                {
                    DataHora = DateTime.Now,
                    IdUsuario = usuario.IdUsuario,
                    Itens = new List<ItemVenda>()
                };

                decimal totalVenda = 0;

                var produtoIds = dto.Itens.Select(i => i.ProdutoId).Distinct().ToList();
                var produtosDict = (await _produtoRepository.GetAllAsync())
                    .Where(p => produtoIds.Contains(p.IdProduto))
                    .ToDictionary(p => p.IdProduto);

                foreach (var itemDto in dto.Itens)
                {
                    if (itemDto.Quantidade <= 0)
                        throw new Exception("A quantidade de cada item deve ser maior que zero.");

                    if (!produtosDict.TryGetValue(itemDto.ProdutoId, out var produto))
                        throw new Exception($"Produto ID {itemDto.ProdutoId} não encontrado.");

                    if (produto.Qtde < itemDto.Quantidade)
                        throw new Exception($"Estoque insuficiente! {produto.Descricao} | Disponível: {produto.Qtde}");

                    var valorItem = produto.PrecoVenda * itemDto.Quantidade;
                    var desconto = itemDto.Desconto ?? 0;
                    var valorTotalItem = valorItem - desconto;

                    var item = new ItemVenda
                    {
                        IdProduto = itemDto.ProdutoId,
                        Qdte = itemDto.Quantidade,
                        PrecoUnitario = produto.PrecoVenda,
                        ValorDesconto = desconto,
                        ValorTotal = valorTotalItem
                    };

                    venda.Itens.Add(item);
                    totalVenda += valorTotalItem;

                    produto.Qtde -= itemDto.Quantidade;
                    await _produtoRepository.UpdateAsync(produto);
                }

                // Desconto da venda inteira: dividido entre os itens, proporcional ao valor de cada um
                var descontoVenda = dto.Desconto ?? 0;

                if (descontoVenda < 0)
                    throw new Exception("O desconto não pode ser negativo.");

                if (descontoVenda > totalVenda)
                    throw new Exception("O desconto não pode ser maior que o total da venda.");

                if (descontoVenda > 0)
                {
                    var restante = descontoVenda;
                    var ultimoItem = venda.Itens.Last();

                    foreach (var item in venda.Itens)
                    {
                        // O último item recebe o que sobrar, para a soma dar exatamente o desconto
                        var parte = item == ultimoItem
                            ? Math.Min(restante, item.ValorTotal)
                            : Math.Round(descontoVenda * item.ValorTotal / totalVenda, 2);

                        item.ValorDesconto += parte;
                        item.ValorTotal -= parte;
                        restante -= parte;
                    }

                    totalVenda = venda.Itens.Sum(i => i.ValorTotal);
                }

                venda.TotalVenda = totalVenda;
                venda.TotalDesconto = venda.Itens.Sum(i => i.ValorDesconto);

                // Busca nome do cliente da tabela PESSOA
                string nomeCliente = "Venda à Vista";

                if (dto.ClienteId.HasValue)
                {
                    var pessoa = await _context.Pessoas
                        .FirstOrDefaultAsync(p => p.IdPessoa == dto.ClienteId.Value);

                    if (pessoa != null)
                    {
                        nomeCliente = pessoa.Nome;
                        venda.IdPessoa = pessoa.IdPessoa;
                    }
                    else
                    {
                        nomeCliente = "Cliente não encontrado";
                    }
                }

                // Salva a venda primeiro
                await _vendaRepository.AddAsync(venda);

                if (dto.IsFiado)
                {
                    // Fiado: o módulo registra a dívida; os pagamentos entram depois (Fiado/pagar)
                    await _fiado.RegistrarVendaFiadaAsync(venda);
                }
                else
                {
                    // Registra o pagamento da venda (venda à vista é paga por inteiro)
                    var pagamento = new FormaPagamento
                    {
                        IdVenda = venda.IdVenda,
                        DataHoraPgto = DateTime.Now,
                        TotalPgto = totalVenda,
                        IdTipoPgto = tipoPagamento!.IdTipoPgto
                    };
                    _context.FormaPagamentos.Add(pagamento);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                // Retorna a resposta com os dados completos
                return new VendaResponseDto
                {
                    IdVenda = venda.IdVenda,
                    DataHora = venda.DataHora,
                    TotalVenda = venda.TotalVenda,
                    TotalDesconto = venda.TotalDesconto,
                    TotalBruto = venda.TotalVenda + venda.TotalDesconto,
                    IdCliente = venda.IdPessoa,
                    FormaPagamento = PagamentoHelper.NomeDaForma(
                        tipoPagamento != null ? new[] { tipoPagamento.Descricao } : Array.Empty<string>(), dto.IsFiado),
                    IsFiado = dto.IsFiado,
                    NomeCliente = nomeCliente,
                    Itens = venda.Itens.Select(i => new ItemVendaResponseDto
                    {
                        ProdutoId = i.IdProduto,
                        NomeProduto = produtosDict[i.IdProduto].Descricao,
                        Quantidade = i.Qdte,
                        PrecoUnitario = i.PrecoUnitario,
                        ValorDesconto = i.ValorDesconto,
                        ValorTotal = i.ValorTotal
                    }).ToList()
                };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<VendaResponseDto>> GetAllAsync()
        {
            var vendas = await _vendaRepository.GetAllAsync();

            // Carrega uma vez só o que todas as vendas precisam (produtos, pagamentos e fiados)
            var produtosDict = (await _produtoRepository.GetAllAsync()).ToDictionary(p => p.IdProduto);

            var pagamentos = (await _context.FormaPagamentos
                .Include(fp => fp.TipoPagamento)
                .AsNoTracking()
                .ToListAsync())
                .GroupBy(fp => fp.IdVenda)
                .ToDictionary(g => g.Key, g => g.ToList());

            var fiadas = await _fiado.VendasFiadasAsync();

            return vendas.Select(v => MapToResponse(v, produtosDict, pagamentos, fiadas)).ToList();
        }

        public async Task<VendaResponseDto?> GetByIdAsync(int id)
        {
            var venda = await _vendaRepository.GetByIdAsync(id);
            if (venda == null) return null;

            var produtosDict = (await _produtoRepository.GetAllAsync()).ToDictionary(p => p.IdProduto);

            var pagamentos = (await _context.FormaPagamentos
                .Include(fp => fp.TipoPagamento)
                .AsNoTracking()
                .Where(fp => fp.IdVenda == id)
                .ToListAsync())
                .GroupBy(fp => fp.IdVenda)
                .ToDictionary(g => g.Key, g => g.ToList());

            var fiadas = await _fiado.VendasFiadasAsync();

            return MapToResponse(venda, produtosDict, pagamentos, fiadas);
        }

        private static VendaResponseDto MapToResponse(
            Venda v,
            Dictionary<int, Produto> produtosDict,
            Dictionary<int, List<FormaPagamento>> pagamentos,
            HashSet<int> fiadas)
        {
            var isFiado = fiadas.Contains(v.IdVenda);

            var tipos = pagamentos.ContainsKey(v.IdVenda)
                ? pagamentos[v.IdVenda].Select(p => p.TipoPagamento?.Descricao ?? "—")
                : Enumerable.Empty<string>();

            return new VendaResponseDto
            {
                IdVenda = v.IdVenda,
                DataHora = v.DataHora,
                TotalVenda = v.TotalVenda,
                TotalDesconto = v.TotalDesconto,
                TotalBruto = v.TotalVenda + v.TotalDesconto,
                IdCliente = v.IdPessoa,
                // A venda à vista não possui cliente
                NomeCliente = v.Pessoa?.Nome ?? "Venda à Vista",
                FormaPagamento = PagamentoHelper.NomeDaForma(tipos, isFiado),
                IsFiado = isFiado,
                Itens = v.Itens.Select(i => new ItemVendaResponseDto
                {
                    ProdutoId = i.IdProduto,
                    NomeProduto = produtosDict.ContainsKey(i.IdProduto) ? produtosDict[i.IdProduto].Descricao : $"Produto {i.IdProduto}",
                    Quantidade = i.Qdte,
                    PrecoUnitario = i.PrecoUnitario,
                    ValorDesconto = i.ValorDesconto,
                    ValorTotal = i.ValorTotal
                }).ToList()
            };
        }
    }
}
