namespace SistemaWhiteLabel.API.DTOs
{
    public class ComprovanteVendaDto
    {
        public int IdVenda { get; set; }
        public DateTime DataVenda { get; set; }
        public string NomeCliente { get; set; } = string.Empty;
        public string CpfCliente { get; set; } = string.Empty;
        public List<ItemVendaComprovanteDto> Itens { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal Desconto { get; set; }
        public decimal Total { get; set; }
        public string FormaPagamento { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal ValorPago { get; set; }
        public decimal SaldoRestante { get; set; }
    }

    public class ItemVendaComprovanteDto
    {
        public string Produto { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal Desconto { get; set; }
        public decimal Total { get; set; }
    }

    public class ComprovantePagamentoDto
    {
        public int IdPagamento { get; set; }
        public int IdVenda { get; set; }
        public DateTime DataPagamento { get; set; }
        public string NomeCliente { get; set; } = string.Empty;
        public string CpfCliente { get; set; } = string.Empty;
        public decimal ValorPago { get; set; }
        public decimal SaldoAnterior { get; set; }
        public decimal SaldoAtual { get; set; }
        public string FormaPagamento { get; set; } = string.Empty;
        public string Operador { get; set; } = string.Empty;
    }
}