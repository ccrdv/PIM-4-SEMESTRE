using Microsoft.AspNetCore.Mvc;
using SistemaWhiteLabel.API.DTOs;
using SistemaWhiteLabel.API.Data;
using SistemaWhiteLabel.API.Helpers;
using SistemaWhiteLabel.API.Modules.Fiado;
using Microsoft.EntityFrameworkCore;

namespace SistemaWhiteLabel.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IFiadoGateway _fiado;

        public DashboardController(ApplicationDbContext context, IFiadoGateway fiado)
        {
            _context = context;
            _fiado = fiado;
        }

        /// <summary>
        /// Resumo do período: today, 7, 30, month (este mês) ou prev (mês anterior)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<DashboardDto>> GetDashboard([FromQuery] string periodo = "30")
        {
            // 1. Período (De inclusive, Ate exclusivo)
            var hoje = DateTime.Today;
            var de = hoje;
            var ate = hoje.AddDays(1);

            switch (periodo)
            {
                case "today":
                    break;
                case "7":
                    de = hoje.AddDays(-6);
                    break;
                case "month":
                    de = new DateTime(hoje.Year, hoje.Month, 1);
                    break;
                case "prev":
                    ate = new DateTime(hoje.Year, hoje.Month, 1);
                    de = ate.AddMonths(-1);
                    break;
                default:
                    periodo = "30";
                    de = hoje.AddDays(-29);
                    break;
            }

            // 2. Vendas do período, com itens, produtos e categorias
            var vendas = await _context.Vendas
                .AsNoTracking()
                .Include(v => v.Itens)
                    .ThenInclude(i => i.Produto)
                        .ThenInclude(p => p!.Categoria)
                .Where(v => v.DataHora >= de && v.DataHora < ate)
                .ToListAsync();

            // 3. Forma de pagamento de cada venda
            var pagamentos = await (from fp in _context.FormaPagamentos
                                    join v in _context.Vendas on fp.IdVenda equals v.IdVenda
                                    where v.DataHora >= de && v.DataHora < ate
                                    select new { fp.IdVenda, Tipo = fp.TipoPagamento!.Descricao })
                                   .ToListAsync();

            var tiposPorVenda = pagamentos
                .GroupBy(p => p.IdVenda)
                .ToDictionary(g => g.Key, g => g.Select(p => p.Tipo).ToList());

            var fiadas = await _fiado.VendasFiadasAsync();

            // 4. Totais
            var liquido = vendas.Sum(v => v.TotalVenda);
            var descontos = vendas.Sum(v => v.TotalDesconto);
            var bruto = liquido + descontos;

            var dashboard = new DashboardDto
            {
                Periodo = periodo,
                De = de,
                Ate = ate.AddDays(-1),
                QuantidadeVendas = vendas.Count,
                ProdutosVendidos = vendas.Sum(v => v.Itens.Sum(i => i.Qdte)),
                FaturamentoLiquido = liquido,
                FaturamentoBruto = bruto,
                TotalDescontos = descontos,
                TicketMedioLiquido = vendas.Count > 0 ? liquido / vendas.Count : 0,
                TicketMedioBruto = vendas.Count > 0 ? bruto / vendas.Count : 0,
                FiadoAtivo = await _fiado.EstaAtivoAsync(),
                TotalFiado = await _fiado.TotalAReceberAsync(),
                VendasFiadasPeriodo = vendas.Where(v => fiadas.Contains(v.IdVenda)).Sum(v => v.TotalVenda)
            };

            // 5. Estoque (mínimo de cada produto)
            dashboard.ProdutosSemEstoque = await _context.Produtos.CountAsync(p => p.Qtde == 0);
            dashboard.ProdutosBaixoEstoque = await _context.Produtos.CountAsync(p => p.Qtde > 0 && p.Qtde <= p.EstoqueMinimo);

            // 6. Vendas por dia
            dashboard.VendasPorDia = vendas
                .GroupBy(v => v.DataHora.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DashboardDiaDto
                {
                    Data = g.Key,
                    Quantidade = g.Count(),
                    Liquido = g.Sum(v => v.TotalVenda),
                    Bruto = g.Sum(v => v.TotalVenda + v.TotalDesconto)
                })
                .ToList();

            // 7. Por produto e por categoria (bruto do item = valor do item + desconto do item)
            var itens = vendas.SelectMany(v => v.Itens).ToList();

            dashboard.PorProduto = itens
                .GroupBy(i => i.IdProduto)
                .Select(g => new DashboardProdutoDto
                {
                    IdProduto = g.Key,
                    Nome = g.First().Produto?.Descricao ?? $"Produto {g.Key}",
                    Quantidade = g.Sum(i => i.Qdte),
                    Liquido = g.Sum(i => i.ValorTotal),
                    Bruto = g.Sum(i => i.ValorTotal + i.ValorDesconto)
                })
                .OrderByDescending(p => p.Quantidade)
                .ToList();

            dashboard.PorCategoria = itens
                .GroupBy(i => i.Produto?.Categoria?.Descricao ?? "Sem categoria")
                .Select(g => new DashboardCategoriaDto
                {
                    Nome = g.Key,
                    Liquido = g.Sum(i => i.ValorTotal),
                    Bruto = g.Sum(i => i.ValorTotal + i.ValorDesconto)
                })
                .OrderByDescending(c => c.Liquido)
                .ToList();

            // 8. Por forma de pagamento (cada venda conta uma vez, na forma dela)
            dashboard.PorPagamento = vendas
                .GroupBy(v => PagamentoHelper.NomeDaForma(
                    tiposPorVenda.ContainsKey(v.IdVenda) ? tiposPorVenda[v.IdVenda] : new List<string>(),
                    fiadas.Contains(v.IdVenda)))
                .Select(g => new DashboardPagamentoDto
                {
                    Forma = g.Key,
                    Quantidade = g.Count(),
                    Liquido = g.Sum(v => v.TotalVenda),
                    Bruto = g.Sum(v => v.TotalVenda + v.TotalDesconto)
                })
                .OrderByDescending(p => p.Liquido)
                .ToList();

            return Ok(dashboard);
        }
    }
}
