namespace SistemaWhiteLabel.API.Modules.Fiado
{
    // Saldo calculado de um fiado (Valor_Original - pagamentos)
    public class FiadoSaldo
    {
        public int IdVenda { get; set; }
        public int IdPessoa { get; set; }
        public string NomeCliente { get; set; } = string.Empty;
        public DateTime DataHora { get; set; }
        public decimal ValorOriginal { get; set; }
        public decimal Saldo { get; set; }
    }

    public interface IFiadoService
    {
        Task<List<FiadoSaldo>> CalcularSaldosAsync();             // todos os fiados com o saldo de cada um
        Task<TotalFiadosDto> GetResumoAsync();
        Task<List<FiadoResponseDto>> GetPendentesAsync();
        Task<PagamentoResponseDto> RegistrarPagamentoAsync(PagamentoFiadoDto dto);
        Task<HistoricoFiadoClienteDto?> GetHistoricoClienteAsync(int idPessoa);
    }
}
