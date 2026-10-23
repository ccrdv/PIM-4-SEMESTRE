using SistemaWhiteLabel.API.DTOs;

namespace SistemaWhiteLabel.API.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginEtapa1Dto?> LoginAsync(LoginDto loginDto);
        Task<ConfigurarDoisFatoresDto> ConfigurarDoisFatoresAsync(string username);
        Task<TokenDto?> AtivarDoisFatoresAsync(string username, AtivarDoisFatoresDto dto);
        Task<TokenDto?> VerificarDoisFatoresAsync(string username, string codigo);
    }
}
