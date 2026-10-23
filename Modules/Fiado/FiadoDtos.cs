namespace SistemaWhiteLabel.API.Modules.Fiado
{
    public class FiadoResponseDto
    {
        public int IdVenda { get; set; }
        public int IdCliente { get; set; }
        public DateTime DataHora { get; set; }
        public string NomeCliente { get; set; } = string.Empty;
        public decimal ValorTotal { get; set; }
        public decimal ValorPendente { get; set; }
        public string Status { get; set; } = "Pendente";
    }

    public class TotalFiadosDto
    {
        public decimal TotalAReceber { get; set; }
        public int QuantidadeClientesDevedores { get; set; }
        public int QuantidadeVendasPendentes { get; set; }
        public List<FiadoResponseDto> VendasFiadas { get; set; } = new();
        public decimal TotalRecebidoHoje { get; set; }
    }

    public class PagamentoFiadoDto
    {
        public int IdVenda { get; set; }
        public decimal ValorPago { get; set; }
        public string? FormaPagamento { get; set; }      // Dinheiro, Pix, Cartão... (padrão: Dinheiro)
        public string? Observacao { get; set; }
    }

    public class PagamentoResponseDto
    {
        public int IdVenda { get; set; }
        public string NomeCliente { get; set; } = string.Empty;
        public decimal ValorTotalVenda { get; set; }
        public decimal ValorPago { get; set; }
        public decimal ValorRestante { get; set; }
        public string Mensagem { get; set; } = string.Empty;
    }

    public class HistoricoFiadoClienteDto
    {
        public int IdCliente { get; set; }
        public string NomeCliente { get; set; } = string.Empty;
        public string CpfCliente { get; set; } = string.Empty;
        public decimal TotalGeral { get; set; }
        public decimal TotalPago { get; set; }
        public decimal SaldoAtual { get; set; }
        public List<TransacaoFiadoDto> Transacoes { get; set; } = new();
    }

    public class TransacaoFiadoDto
    {
        public int Id { get; set; }
        public DateTime Data { get; set; }
        public string Tipo { get; set; } = string.Empty;     // DÉBITO ou PAGAMENTO
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public decimal SaldoApos { get; set; }
        public int? IdVenda { get; set; }
        public int? IdPagamento { get; set; }
    }
}
