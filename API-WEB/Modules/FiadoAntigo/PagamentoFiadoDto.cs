namespace SistemaWhiteLabel.API.DTOs
{
    public class PagamentoFiadoDto
    {
        public int IdVenda { get; set; }
        public decimal ValorPago { get; set; }
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
}