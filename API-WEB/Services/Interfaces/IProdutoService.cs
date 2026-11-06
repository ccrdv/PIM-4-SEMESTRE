using SistemaWhiteLabel.API.DTOs;
using SistemaWhiteLabel.API.Models;

namespace SistemaWhiteLabel.API.Services.Interfaces
{
    public interface IProdutoService
    {
        Task<IEnumerable<ProdutoDto>> GetAllAsync();
        Task<ProdutoDto?> GetByIdAsync(int id);
        Task<ProdutoDto> CreateAsync(ProdutoCreateDto dto);
        Task<IEnumerable<ProdutoDto>> BuscarAsync(string termo);
        Task UpdateAsync(int id, ProdutoCreateDto dto);
        Task DeleteAsync(int id);
    }
}