namespace SistemaWhiteLabel.API.Modules.Fornecedor
{
    public interface IFornecedorService
    {
        Task<List<FornecedorDto>> ListarAsync();
        Task<FornecedorDto?> GetByIdAsync(int id);
        Task<FornecedorDto> CriarAsync(FornecedorSalvarDto dto);
        Task<FornecedorDto?> AtualizarAsync(int id, FornecedorSalvarDto dto);
        Task<bool> ExcluirAsync(int id);
    }
}
