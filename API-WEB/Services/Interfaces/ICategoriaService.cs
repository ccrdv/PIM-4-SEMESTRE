using SistemaWhiteLabel.API.DTOs;

namespace SistemaWhiteLabel.API.Services.Interfaces
{
    public interface ICategoriaService
    {
        Task<IEnumerable<CategoriaDto>> GetAllAsync();
        Task<CategoriaDto?> GetByIdAsync(int id);
        Task<CategoriaDto> CreateAsync(CategoriaCreateDto dto);
        Task UpdateAsync(int id, CategoriaCreateDto dto);
        Task DeleteAsync(int id);
    }
}