namespace SistemaWhiteLabel.API.DTOs
{
    public class FiadoResponseDto
    {
        public int IdVenda { get; set; }
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
}