using SistemaWhiteLabel.API.Models;

namespace SistemaWhiteLabel.API.Repositories.Interfaces
{
    public interface IProdutoRepository
    {
        Task<IEnumerable<Produto>> GetAllAsync();

        Task<Produto?> GetByIdAsync(int id);
        Task AddAsync(Produto produto);
        Task UpdateAsync(Produto produto);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}