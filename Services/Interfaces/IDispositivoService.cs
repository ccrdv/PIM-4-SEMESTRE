using SistemaWhiteLabel.API.DTOs;

namespace SistemaWhiteLabel.API.Services.Interfaces
{
    public interface IDispositivoService
    {
        PareamentoDto IniciarPareamento(string username);
        Task<ParearResultadoDto> ParearAsync(string codigo, string? nomeDispositivo, string ip);
        Task<IEnumerable<DispositivoResponseDto>> ListarAsync(string username);
        Task<DispositivoResponseDto?> RenovarAsync(int id, string username);
        Task<bool> RevogarAsync(int id, string username);
        Task<MinhaSessaoDto?> MinhaSessaoAsync(int idDispositivo);
    }
}
