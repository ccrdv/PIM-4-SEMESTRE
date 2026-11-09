namespace SistemaWhiteLabel.API.Modules.Fornecedor
{
    public interface ICompraService
    {
        Task<List<CompraResponseDto>> ListarAsync(int? idFornecedor);
        Task<CompraResponseDto?> GetByIdAsync(int id);
        Task<ResumoComprasDto> GetResumoAsync();
        Task<CompraResponseDto> RegistrarAsync(NovaCompraDto dto, string username);
        Task<CompraResponseDto> ReceberAsync(int id);
        Task<CompraResponseDto> CancelarAsync(int id);
    }
}
