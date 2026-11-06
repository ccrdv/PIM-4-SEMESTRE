using SistemaWhiteLabel.API.DTOs;

namespace SistemaWhiteLabel.API.Services.Interfaces
{
    public interface IModuloService
    {
        Task<bool> EstaAtivoAsync(string nome);
        Task<IEnumerable<ModuloDto>> ListarAsync();
        Task<ModuloDto?> AlterarAsync(string nome, bool ativo);
    }
}
