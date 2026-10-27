using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaWhiteLabel.API.DTOs;
using SistemaWhiteLabel.API.Data;
using Microsoft.EntityFrameworkCore;
using SistemaWhiteLabel.API.Modules.Fiado;

namespace SistemaWhiteLabel.API.Services
{
    public class ComprovanteService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFiadoGateway _fiado;

        public ComprovanteService(ApplicationDbContext context, IFiadoGateway fiado)
        {
            _context = context;
            _fiado = fiado;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GerarComprovanteVendaAsync(int idVenda)
        {
            var venda = await _context.Vendas
                .Include(v => v.Itens)
                    .ThenInclude(i => i.Produto)
                .Include(v => v.Pessoa)
                .FirstOrDefaultAsync(v => v.IdVenda == idVenda);

            if (venda == null)
                throw new Exception("Venda não encontrada");

            var pagamentos = await _context.FormaPagamentos
                .Include(fp => fp.TipoPagamento)
                .Where(fp => fp.IdVenda == idVenda)
                .ToListAsync();

            // CORREÇÃO: Usar diretamente (decimal não é nullable)
            var totalVenda = venda.TotalVenda;
            var totalPago = pagamentos.Sum(p => p.TotalPgto ?? 0);
            var saldoRestante = totalVenda - totalPago;

            // Venda a prazo sem nenhum pagamento ainda
            var isFiado = (await _fiado.VendasFiadasAsync()).Contains(idVenda);

            var dados = new ComprovanteVendaDto
            {
                IdVenda = venda.IdVenda,
                DataVenda = venda.DataHora,
                NomeCliente = venda.Pessoa?.Nome ?? "CONSUMIDOR FINAL",
                CpfCliente = venda.Pessoa?.Cpf ?? "---",
                Itens = venda.Itens.Select(i => new ItemVendaComprovanteDto
                {
                    Produto = i.Produto?.Descricao ?? "Produto não identificado",
                    Quantidade = i.Qdte,
                    PrecoUnitario = i.PrecoUnitario,
                    Desconto = i.ValorDesconto,
                    Total = i.ValorTotal
                }).ToList(),
                Subtotal = venda.Itens.Sum(i => i.ValorTotal),
                Desconto = venda.TotalDesconto,
                Total = totalVenda,
                FormaPagamento = pagamentos.Any()
                    ? string.Join(" / ", pagamentos.Select(p => p.TipoPagamento?.Descricao ?? "---").Distinct()).ToUpper()
                    : (isFiado ? "A PRAZO" : "SEM PAGAMENTO"),
                Status = saldoRestante > 0 ? "PARCIALMENTE PAGO" : "PAGO",
                ValorPago = totalPago,
                SaldoRestante = saldoRestante
            };

            return GerarPdfVenda(dados);
        }

        public async Task<byte[]> GerarComprovantePagamentoAsync(int idVenda, decimal valorPago, decimal saldoAnterior)
        {
            var venda = await _context.Vendas
                .Include(v => v.Pessoa)
                .FirstOrDefaultAsync(v => v.IdVenda == idVenda);

            if (venda == null)
                throw new Exception("Venda não encontrada");

            var pagamentos = await _context.FormaPagamentos
                .Where(fp => fp.IdVenda == idVenda)
                .ToListAsync();

            var totalVenda = venda.TotalVenda;
            var totalPagoAteAgora = pagamentos.Sum(p => p.TotalPgto ?? 0);
            var saldoAtual = totalVenda - totalPagoAteAgora;

            var dados = new ComprovantePagamentoDto
            {
                IdPagamento = pagamentos.Count + 1,
                IdVenda = idVenda,
                DataPagamento = DateTime.Now,
                NomeCliente = venda.Pessoa?.Nome ?? "Cliente não identificado",
                CpfCliente = venda.Pessoa?.Cpf ?? "---",
                ValorPago = valorPago,
                SaldoAnterior = saldoAnterior,
                SaldoAtual = saldoAtual,
                FormaPagamento = "DINHEIRO",
                Operador = "SISTEMA"
            };

            return GerarPdfPagamento(dados);
        }

        private byte[] GerarPdfVenda(ComprovanteVendaDto dados)
        {
            using var stream = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A6);
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .AlignCenter()
                        .Text(txt =>
                        {
                            txt.Span("SISTEMA WHITE LABEL").Bold().FontSize(14);
                            txt.EmptyLine();
                            txt.Span("COMPROVANTE DE VENDA").Bold().FontSize(12);
                        });

                    page.Content()
                        .PaddingVertical(10)
                        .Column(col =>
                        {
                            col.Item().Text($"Nº: {dados.IdVenda:000000}").Bold();
                            col.Item().Text($"Data: {dados.DataVenda:dd/MM/yyyy HH:mm}");
                            col.Item().Text($"Cliente: {dados.NomeCliente}");
                            col.Item().Text($"CPF: {dados.CpfCliente}");
                            col.Item().PaddingTop(5).LineHorizontal(0.5f);

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.ConstantColumn(20);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("PRODUTO").Bold();
                                    header.Cell().Text("QTD").Bold().AlignCenter();
                                    header.Cell().Text("TOTAL").Bold().AlignRight();
                                });

                                foreach (var item in dados.Itens)
                                {
                                    table.Cell().Text(item.Produto);
                                    table.Cell().Text(item.Quantidade.ToString()).AlignCenter();
                                    table.Cell().Text($"R$ {item.Total:F2}").AlignRight();
                                }
                            });

                            col.Item().PaddingTop(5).LineHorizontal(0.5f);

                            col.Item().AlignRight().Column(inner =>
                            {
                                inner.Item().Text($"Subtotal: R$ {dados.Subtotal:F2}");
                                inner.Item().Text($"Desconto: R$ {dados.Desconto:F2}");
                                inner.Item().Text($"TOTAL: R$ {dados.Total:F2}").Bold();
                            });

                            col.Item().PaddingTop(10).LineHorizontal(0.5f);
                            col.Item().Text($"Pagamento: {dados.FormaPagamento}");
                            col.Item().Text($"Valor Pago: R$ {dados.ValorPago:F2}");

                            if (dados.SaldoRestante > 0)
                            {
                                col.Item().Text($"Saldo Restante: R$ {dados.SaldoRestante:F2}").FontColor(Colors.Red.Medium);
                                col.Item().Text($"Status: {dados.Status}").FontColor(Colors.Orange.Medium);
                            }
                            else
                            {
                                col.Item().Text($"Status: {dados.Status}").FontColor(Colors.Green.Medium);
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(8);
                });
            }).GeneratePdf(stream);

            return stream.ToArray();
        }

        private byte[] GerarPdfPagamento(ComprovantePagamentoDto dados)
        {
            using var stream = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A6);
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .AlignCenter()
                        .Text(txt =>
                        {
                            txt.Span("SISTEMA WHITE LABEL").Bold().FontSize(14);
                            txt.EmptyLine();
                            txt.Span("RECIBO DE PAGAMENTO").Bold().FontSize(12);
                        });

                    page.Content()
                        .PaddingVertical(10)
                        .Column(col =>
                        {
                            col.Item().Text($"RECIBO Nº: {dados.IdPagamento:000000}").Bold();
                            col.Item().Text($"VENDA Nº: {dados.IdVenda:000000}");
                            col.Item().Text($"Data: {dados.DataPagamento:dd/MM/yyyy HH:mm}");
                            col.Item().PaddingTop(5).LineHorizontal(0.5f);
                            col.Item().Text($"Cliente: {dados.NomeCliente}");
                            col.Item().Text($"CPF: {dados.CpfCliente}");
                            col.Item().PaddingTop(5).LineHorizontal(0.5f);
                            col.Item().Text($"VALOR RECEBIDO: R$ {dados.ValorPago:F2}").Bold().FontColor(Colors.Green.Darken2);
                            col.Item().PaddingTop(5).LineHorizontal(0.5f);
                            col.Item().Text($"Saldo Anterior: R$ {dados.SaldoAnterior:F2}");
                            col.Item().Text($"Saldo Atual: R$ {dados.SaldoAtual:F2}").Bold();
                            col.Item().Text($"Forma de Pagamento: {dados.FormaPagamento}");
                            col.Item().PaddingTop(10).LineHorizontal(0.5f);
                            col.Item().Text($"Operador: {dados.Operador}");

                            if (dados.SaldoAtual <= 0)
                            {
                                col.Item().PaddingTop(10).Text("DÉBITO TOTALMENTE QUITADO!").Bold().FontColor(Colors.Green.Darken2);
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(8);
                });
            }).GeneratePdf(stream);

            return stream.ToArray();
        }
    }
}