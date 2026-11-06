using Microsoft.AspNetCore.Mvc;
using SistemaWhiteLabel.API.DTOs;
using SistemaWhiteLabel.API.Data;
using Microsoft.EntityFrameworkCore;
using SistemaWhiteLabel.API.Models;

namespace SistemaWhiteLabel.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FiadoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FiadoController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retorna resumo completo dos fiados
        /// </summary>
        [HttpGet("resumo")]
        public async Task<ActionResult<TotalFiadosDto>> GetResumoFiados()
        {
            // 1. Buscar clientes com débito (TOTAL_FIADO > 0)
            var clientesDevedores = await _context.Clientes
                .Include(c => c.Pessoa)
                .Where(c => c.TotalFiado > 0 && c.Status == "ATIVO")
                .ToListAsync();

            var totalAReceber = clientesDevedores.Sum(c => c.TotalFiado);
            var quantidadeClientesDevedores = clientesDevedores.Count;

            // 2. Buscar vendas fiadas com saldo pendente > 0 
            var todasVendasFiadas = await _context.Vendas
                .Include(v => v.Cliente)
                    .ThenInclude(c => c!.Pessoa) 
                .Where(v => v.ClienteIdCliente != null)
                .ToListAsync();

            // Calcular saldo pendente para cada venda
            var vendasPendentes = new List<FiadoResponseDto>();

            foreach (var venda in todasVendasFiadas)
            {
                var totalPago = await _context.FormaPagamentos
                    .Where(fp => fp.IdVenda == venda.IdVenda)
                    .SumAsync(fp => fp.TotalPgto ?? 0);

                var saldoPendente = venda.TotalVenda - totalPago;

                if (saldoPendente > 0)
                {
                    var nomeCliente = venda.Cliente?.Pessoa?.Nome;

                    // Se ainda não conseguiu o nome, buscar diretamente
                    if (string.IsNullOrEmpty(nomeCliente) && venda.ClienteIdCliente.HasValue)
                    {
                        var pessoa = await _context.Pessoas
                            .FirstOrDefaultAsync(p => p.ClienteIdCliente == venda.ClienteIdCliente.Value);
                        nomeCliente = pessoa?.Nome ?? "Cliente não identificado";
                    }

                    vendasPendentes.Add(new FiadoResponseDto
                    {
                        IdVenda = venda.IdVenda,
                        DataHora = venda.DataHora,
                        NomeCliente = nomeCliente ?? "Cliente não identificado",
                        ValorTotal = venda.TotalVenda,
                        ValorPendente = saldoPendente,
                        Status = "Pendente"
                    });
                }
            }

            vendasPendentes = vendasPendentes.OrderByDescending(v => v.DataHora).Take(50).ToList();

            // 3. Calcular total recebido hoje
            var hoje = DateTime.Today;
            var amanha = hoje.AddDays(1);

            var totalRecebidoHoje = await _context.FormaPagamentos
                .Where(fp => fp.DataHoraPgto >= hoje && fp.DataHoraPgto < amanha)
                .SumAsync(fp => fp.TotalPgto ?? 0);

            return Ok(new TotalFiadosDto
            {
                TotalAReceber = totalAReceber,
                QuantidadeClientesDevedores = quantidadeClientesDevedores,
                QuantidadeVendasPendentes = vendasPendentes.Count,
                VendasFiadas = vendasPendentes,
                TotalRecebidoHoje = totalRecebidoHoje
            });
        }

        [HttpGet]
        public async Task<ActionResult<List<FiadoResponseDto>>> GetAllFiados()
        {
            var todasVendasFiadas = await _context.Vendas
                .Include(v => v.Cliente)
                    .ThenInclude(c => c!.Pessoa)
                .Where(v => v.ClienteIdCliente != null)
                .ToListAsync();

            var fiados = new List<FiadoResponseDto>();

            foreach (var venda in todasVendasFiadas)
            {
                var totalPago = await _context.FormaPagamentos
                    .Where(fp => fp.IdVenda == venda.IdVenda)
                    .SumAsync(fp => fp.TotalPgto ?? 0);

                var saldoPendente = venda.TotalVenda - totalPago;

                if (saldoPendente > 0)
                {
                    var nomeCliente = venda.Cliente?.Pessoa?.Nome;

                    if (string.IsNullOrEmpty(nomeCliente) && venda.ClienteIdCliente.HasValue)
                    {
                        var pessoa = await _context.Pessoas
                            .FirstOrDefaultAsync(p => p.ClienteIdCliente == venda.ClienteIdCliente.Value);
                        nomeCliente = pessoa?.Nome ?? "Cliente não identificado";
                    }

                    fiados.Add(new FiadoResponseDto
                    {
                        IdVenda = venda.IdVenda,
                        DataHora = venda.DataHora,
                        NomeCliente = nomeCliente ?? "Cliente não identificado",
                        ValorTotal = venda.TotalVenda,
                        ValorPendente = saldoPendente,
                        Status = "Pendente"
                    });
                }
            }

            return Ok(fiados.OrderByDescending(f => f.DataHora));
        }

        /// <summary>
        /// Registra pagamento parcial ou total de um fiado
        /// </summary>
        [HttpPost("pagar")]
        public async Task<ActionResult<PagamentoResponseDto>> RegistrarPagamento([FromBody] PagamentoFiadoDto dto)
        {
            if (dto.ValorPago <= 0)
                return BadRequest("O valor pago deve ser maior que zero.");

            // Buscar a venda
            var venda = await _context.Vendas
                .FirstOrDefaultAsync(v => v.IdVenda == dto.IdVenda);

            if (venda == null)
                return NotFound("Venda não encontrada.");

            if (venda.ClienteIdCliente == null)
                return BadRequest("Esta venda não é fiada.");

            // Buscar o cliente
            var cliente = await _context.Clientes
                .Include(c => c.Pessoa)
                .FirstOrDefaultAsync(c => c.IdCliente == venda.ClienteIdCliente);

            if (cliente == null)
                return NotFound("Cliente não encontrado.");

            // Validar valor do pagamento
            if (dto.ValorPago > cliente.TotalFiado)
                return BadRequest($"Valor pago (R$ {dto.ValorPago:F2}) excede o débito total (R$ {cliente.TotalFiado:F2})");

            // Registrar o pagamento na tabela FORMA_PAGAMENTO
            var pagamento = new FormaPagamento
            {
                IdVenda = venda.IdVenda,
                DataHoraPgto = DateTime.Now,
                TotalPgto = dto.ValorPago,
                TipoPgto = "FIADO",
                IdTipoPgto = 1 
            };
            _context.FormaPagamentos.Add(pagamento);

            // Atualizar o TOTAL_FIADO do cliente
            cliente.TotalFiado -= dto.ValorPago;

            await _context.SaveChangesAsync();

            var valorRestante = cliente.TotalFiado;

            return Ok(new PagamentoResponseDto
            {
                IdVenda = venda.IdVenda,
                NomeCliente = cliente.Pessoa?.Nome ?? "Cliente não encontrado",
                ValorTotalVenda = venda.TotalVenda,
                ValorPago = dto.ValorPago,
                ValorRestante = valorRestante,
                Mensagem = valorRestante <= 0
                    ? "Fiado quitado com sucesso!"
                    : $"Pagamento registrado. Restam R$ {valorRestante:F2}"
            });
        }

        [HttpGet("historico/{clienteId}")]
        public async Task<ActionResult<HistoricoFiadoClienteDto>> GetHistoricoCliente(int clienteId)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Pessoa)
                .FirstOrDefaultAsync(c => c.IdCliente == clienteId);

            if (cliente == null)
                return NotFound(new { mensagem = "Cliente não encontrado" });

            // Buscar todas as vendas fiadas do cliente
            var vendas = await _context.Vendas
                .Where(v => v.ClienteIdCliente == clienteId)
                .OrderBy(v => v.DataHora)
                .ToListAsync();

            // Buscar todos os pagamentos
            var idsVendas = vendas.Select(v => v.IdVenda).ToList();
            var pagamentos = await _context.FormaPagamentos
                .Where(fp => idsVendas.Contains(fp.IdVenda ?? 0))
                .OrderBy(fp => fp.DataHoraPgto)
                .ToListAsync();

            var transacoes = new List<TransacaoFiadoDto>();
            var saldoCorrente = 0m;

            // Adicionar vendas
            foreach (var venda in vendas)
            {
                saldoCorrente += venda.TotalVenda;
                transacoes.Add(new TransacaoFiadoDto
                {
                    Id = venda.IdVenda,
                    Data = venda.DataHora,
                    Tipo = "DÉBITO",
                    Descricao = $"Venda Nº {venda.IdVenda}",
                    Valor = venda.TotalVenda,
                    SaldoApos = saldoCorrente,
                    IdVenda = venda.IdVenda
                });
            }

            // Adicionar pagamentos
            foreach (var pagamento in pagamentos)
            {
                saldoCorrente -= pagamento.TotalPgto ?? 0;
                transacoes.Add(new TransacaoFiadoDto
                {
                    Id = pagamento.IdFormaPgto,
                    Data = pagamento.DataHoraPgto,
                    Tipo = "PAGAMENTO",
                    Descricao = $"Pagamento - Venda Nº {pagamento.IdVenda}",
                    Valor = pagamento.TotalPgto ?? 0,
                    SaldoApos = saldoCorrente,
                    IdVenda = pagamento.IdVenda,
                    IdPagamento = pagamento.IdFormaPgto
                });
            }

            transacoes = transacoes.OrderBy(t => t.Data).ToList();

            var totalGeral = vendas.Sum(v => v.TotalVenda);
            var totalPago = pagamentos.Sum(p => p.TotalPgto ?? 0);

            return Ok(new HistoricoFiadoClienteDto
            {
                IdCliente = cliente.IdCliente,
                NomeCliente = cliente.Pessoa?.Nome ?? "Cliente não identificado",
                CpfCliente = cliente.Pessoa?.Cpf ?? "---",
                TotalGeral = totalGeral,
                TotalPago = totalPago,
                SaldoAtual = totalGeral - totalPago,
                Transacoes = transacoes
            });
        }
    }
}