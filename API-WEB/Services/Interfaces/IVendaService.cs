using SistemaWhiteLabel.API.DTOs;

namespace SistemaWhiteLabel.API.Services.Interfaces
{
    public interface IVendaService
    {
        Task<VendaResponseDto> RegistrarVendaAsync(NovaVendaRequestDto dto, string username);
        Task<IEnumerable<VendaResponseDto>> GetAllAsync();
        Task<VendaResponseDto?> GetByIdAsync(int id);
    }
}
