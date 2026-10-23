using SistemaWhiteLabel.API.DTOs;

namespace SistemaWhiteLabel.API.Services.Interfaces
{
    public interface IPessoaService
    {
        Task<PessoaResponseDto> CreateAsync(PessoaCreateDto dto);
        Task<IEnumerable<PessoaResponseDto>> GetAllAsync();
        Task<PessoaResponseDto?> GetByIdAsync(int id);
        Task<PessoaResponseDto?> UpdateAsync(int id, PessoaCreateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<PessoaResponseDto>> BuscarAsync(string termo);
    }
}