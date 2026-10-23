namespace SistemaWhiteLabel.API.DTOs
{
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
        public string Tipo { get; set; } = string.Empty; 
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public decimal SaldoApos { get; set; }
        public int? IdVenda { get; set; }
        public int? IdPagamento { get; set; }
    }
}