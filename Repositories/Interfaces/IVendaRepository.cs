using SistemaWhiteLabel.API.Models;

namespace SistemaWhiteLabel.API.Repositories.Interfaces
{
    public interface IVendaRepository
    {
        Task<Venda> AddAsync(Venda venda);
        Task<Venda?> GetByIdAsync(int id);
        Task<IEnumerable<Venda>> GetAllAsync();
    }
}