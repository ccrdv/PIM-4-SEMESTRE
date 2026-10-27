using Microsoft.EntityFrameworkCore;
using SistemaWhiteLabel.API.Data;
using SistemaWhiteLabel.API.Models;

namespace SistemaWhiteLabel.API.Modules.Fiado
{
    public class FiadoService : IFiadoService
    {
        private readonly ApplicationDbContext _context;

        public FiadoService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ====================== SALDOS ======================

        // Saldo de cada fiado = valor_original - pagamentos da venda (tb_forma_pagamento)
        public async Task<List<FiadoSaldo>> CalcularSaldosAsync()
        {
            var fiados = await _context.Fiados
                .AsNoTracking()
                .Include(f => f.Pessoa)
                .ToListAsync();

            // Soma dos pagamentos de cada venda fiada (join, para não depender de lista de Ids)
            var pagos = await (from fp in _context.FormaPagamentos
                               join f in _context.Fiados on fp.IdVenda equals f.IdVenda
                               group fp by fp.IdVenda into g
                               select new { IdVenda = g.Key, Total = g.Sum(x => x.TotalPgto ?? 0) })
                              .ToDictionaryAsync(x => x.IdVenda, x => x.Total);

            return fiados.Select(f => new FiadoSaldo
            {
                IdVenda = f.IdVenda,
                IdPessoa = f.IdPessoa,
                NomeCliente = f.Pessoa?.Nome ?? "Cliente não identificado",
                DataHora = f.DataHora,
                ValorOriginal = f.ValorOriginal,
                Saldo = f.ValorOriginal - (pagos.ContainsKey(f.IdVenda) ? pagos[f.IdVenda] : 0)
            }).ToList();
        }

        public async Task<List<FiadoResponseDto>> GetPendentesAsync()
        {
            var saldos = await CalcularSaldosAsync();

            return saldos
                .Where(s => s.Saldo > 0)
                .OrderByDescending(s => s.DataHora)
                .Select(s => new FiadoResponseDto
                {
                    IdVenda = s.IdVenda,
                    IdCliente = s.IdPessoa,
                    DataHora = s.DataHora,
                    NomeCliente = s.NomeCliente,
                    ValorTotal = s.ValorOriginal,
                    ValorPendente = s.Saldo,
                    Status = "Pendente"
                })
                .ToList();
        }

        // ====================== RESUMO ======================

        public async Task<TotalFiadosDto> GetResumoAsync()
        {
            var saldos = (await CalcularSaldosAsync()).Where(s => s.Saldo > 0).ToList();

            var vendasPendentes = await GetPendentesAsync();

            // Total recebido hoje (só pagamentos de vendas fiadas)
            var hoje = DateTime.Today;
            var amanha = hoje.AddDays(1);

            var totalRecebidoHoje = await (from fp in _context.FormaPagamentos
                                           join f in _context.Fiados on fp.IdVenda equals f.IdVenda
                                           where fp.DataHoraPgto >= hoje && fp.DataHoraPgto < amanha
                                           select fp.TotalPgto ?? 0)
                                          .SumAsync();

            return new TotalFiadosDto
            {
                TotalAReceber = saldos.Sum(s => s.Saldo),
                QuantidadeClientesDevedores = saldos.Select(s => s.IdPessoa).Distinct().Count(),
                QuantidadeVendasPendentes = vendasPendentes.Count,
                VendasFiadas = vendasPendentes.Take(50).ToList(),
                TotalRecebidoHoje = totalRecebidoHoje
            };
        }

        // ====================== PAGAMENTO ======================

        public async Task<PagamentoResponseDto> RegistrarPagamentoAsync(PagamentoFiadoDto dto)
        {
            if (dto.ValorPago <= 0)
                throw new Exception("O valor pago deve ser maior que zero.");

            var fiado = await _context.Fiados
                .Include(f => f.Pessoa)
                .FirstOrDefaultAsync(f => f.IdVenda == dto.IdVenda);

            if (fiado == null)
                throw new KeyNotFoundException("Esta venda não é a prazo.");

            var jaPago = await _context.FormaPagamentos
                .Where(fp => fp.IdVenda == fiado.IdVenda)
                .SumAsync(fp => fp.TotalPgto ?? 0);

            var saldo = fiado.ValorOriginal - jaPago;

            if (saldo <= 0)
                throw new Exception("Esta venda a prazo já está quitada.");

            if (dto.ValorPago > saldo)
                throw new Exception($"Valor pago (R$ {dto.ValorPago:F2}) excede o débito (R$ {saldo:F2})");

            // Como o cliente pagou (tb_tipo_pagamento). Não faz sentido "pagar a prazo com a prazo".
            var formaPagamento = string.IsNullOrWhiteSpace(dto.FormaPagamento)
                ? "DINHEIRO"
                : dto.FormaPagamento.Trim().ToUpper();

            if (formaPagamento == "FIADO" || formaPagamento == "A PRAZO")
                throw new Exception("Informe como o cliente pagou (Dinheiro, Pix, Cartão...).");

            var tipoPagamento = await _context.TiposPagamento
                .FirstOrDefaultAsync(t => t.Descricao.ToUpper() == formaPagamento);

            if (tipoPagamento == null)
                throw new Exception($"Forma de pagamento '{dto.FormaPagamento}' não cadastrada.");

            _context.FormaPagamentos.Add(new FormaPagamento
            {
                IdVenda = fiado.IdVenda,
                DataHoraPgto = DateTime.Now,
                TotalPgto = dto.ValorPago,
                IdTipoPgto = tipoPagamento.IdTipoPgto
            });

            await _context.SaveChangesAsync();

            var valorRestante = saldo - dto.ValorPago;

            return new PagamentoResponseDto
            {
                IdVenda = fiado.IdVenda,
                NomeCliente = fiado.Pessoa?.Nome ?? "Cliente não encontrado",
                ValorTotalVenda = fiado.ValorOriginal,
                ValorPago = dto.ValorPago,
                ValorRestante = valorRestante,
                Mensagem = valorRestante <= 0
                    ? "Venda a prazo quitada com sucesso!"
                    : $"Pagamento registrado. Restam R$ {valorRestante:F2}"
            };
        }

        // ====================== HISTÓRICO DO CLIENTE ======================

        public async Task<HistoricoFiadoClienteDto?> GetHistoricoClienteAsync(int idPessoa)
        {
            var pessoa = await _context.Pessoas
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdPessoa == idPessoa);

            if (pessoa == null)
                return null;

            var fiados = await _context.Fiados
                .AsNoTracking()
                .Where(f => f.IdPessoa == idPessoa)
                .ToListAsync();

            var pagamentos = await (from fp in _context.FormaPagamentos.AsNoTracking()
                                    join f in _context.Fiados on fp.IdVenda equals f.IdVenda
                                    where f.IdPessoa == idPessoa
                                    select fp)
                                   .ToListAsync();

            var transacoes = new List<TransacaoFiadoDto>();

            foreach (var fiado in fiados)
            {
                transacoes.Add(new TransacaoFiadoDto
                {
                    Id = fiado.IdVenda,
                    Data = fiado.DataHora,
                    Tipo = "DÉBITO",
                    Descricao = $"Venda Nº {fiado.IdVenda}",
                    Valor = fiado.ValorOriginal,
                    IdVenda = fiado.IdVenda
                });
            }

            foreach (var pagamento in pagamentos)
            {
                transacoes.Add(new TransacaoFiadoDto
                {
                    Id = pagamento.IdFormaPgto,
                    Data = pagamento.DataHoraPgto,
                    Tipo = "PAGAMENTO",
                    Descricao = $"Pagamento - Venda Nº {pagamento.IdVenda}",
                    Valor = pagamento.TotalPgto ?? 0,
                    IdVenda = pagamento.IdVenda,
                    IdPagamento = pagamento.IdFormaPgto
                });
            }

            // Saldo corrente, na ordem em que as coisas aconteceram
            transacoes = transacoes.OrderBy(t => t.Data).ToList();

            var saldoCorrente = 0m;
            foreach (var transacao in transacoes)
            {
                saldoCorrente += transacao.Tipo == "DÉBITO" ? transacao.Valor : -transacao.Valor;
                transacao.SaldoApos = saldoCorrente;
            }

            var totalGeral = fiados.Sum(f => f.ValorOriginal);
            var totalPago = pagamentos.Sum(p => p.TotalPgto ?? 0);

            return new HistoricoFiadoClienteDto
            {
                IdCliente = pessoa.IdPessoa,
                NomeCliente = pessoa.Nome,
                CpfCliente = pessoa.Cpf,
                TotalGeral = totalGeral,
                TotalPago = totalPago,
                SaldoAtual = totalGeral - totalPago,
                Transacoes = transacoes
            };
        }
    }
}
