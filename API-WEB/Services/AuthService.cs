using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SistemaWhiteLabel.API.Data;
using SistemaWhiteLabel.API.DTOs;
using SistemaWhiteLabel.API.Helpers;
using SistemaWhiteLabel.API.Models;
using SistemaWhiteLabel.API.Services.Interfaces;

namespace SistemaWhiteLabel.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly ITwoFactorService _twoFactorService;
        private readonly IMemoryCache _cache;

        private const string FALHAS_PREFIX = "falhas_login_";
        private const string PASSO_2FA_PREFIX = "ultimo_passo_2fa_";
        private const int MAX_TENTATIVAS = 5;
        private const int BLOQUEIO_MINUTOS = 5;

        public AuthService(
            ApplicationDbContext context,
            ITokenService tokenService,
            ITwoFactorService twoFactorService,
            IMemoryCache cache)
        {
            _context = context;
            _tokenService = tokenService;
            _twoFactorService = twoFactorService;
            _cache = cache;
        }

        // ETAPA 1: confere usuário e senha e devolve o token temporário do 2FA
        public async Task<LoginEtapa1Dto?> LoginAsync(LoginDto loginDto)
        {
            VerificarBloqueio(loginDto.Username);

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

            if (usuario == null || usuario.Status != "Ativo" || usuario.Senha != loginDto.Senha)
            {
                RegistrarFalha(loginDto.Username);
                return null;
            }

            return new LoginEtapa1Dto
            {
                TokenTemporario = _tokenService.GenerateTwoFactorToken(usuario.Username),
                ExpiraEm = DateTime.UtcNow.AddMinutes(_tokenService.GetTwoFactorExpirationMinutes()),
                DoisFatoresConfigurado = !string.IsNullOrEmpty(usuario.Chave2FA),
                Username = usuario.Username
            };
        }

        // ETAPA 2 (primeiro acesso): gera a chave que será cadastrada no app autenticador
        public async Task<ConfigurarDoisFatoresDto> ConfigurarDoisFatoresAsync(string username)
        {
            var usuario = await BuscarUsuarioAtivoAsync(username);

            if (!string.IsNullOrEmpty(usuario.Chave2FA))
                throw new Exception("O 2FA já está configurado para este usuário.");

            var chave = _twoFactorService.GerarChave();

            return new ConfigurarDoisFatoresDto
            {
                Chave = chave,
                UriOtpAuth = _twoFactorService.GerarUriOtpAuth(usuario.Username, chave)
            };
        }

        // ETAPA 2 (primeiro acesso): confirma o primeiro código, grava a chave e abre a sessão
        public async Task<TokenDto?> AtivarDoisFatoresAsync(string username, AtivarDoisFatoresDto dto)
        {
            VerificarBloqueio(username);

            var usuario = await BuscarUsuarioAtivoAsync(username);

            if (!string.IsNullOrEmpty(usuario.Chave2FA))
                throw new Exception("O 2FA já está configurado para este usuário.");

            if (!_twoFactorService.ChaveValida(dto.Chave))
                throw new Exception("Chave do autenticador inválida. Volte e refaça o login.");

            if (!_twoFactorService.ValidarCodigo(dto.Chave, dto.Codigo, out var passo))
            {
                RegistrarFalha(username);
                return null;
            }

            usuario.Chave2FA = dto.Chave;
            await _context.SaveChangesAsync();

            MarcarPassoUsado(username, passo);
            LimparFalhas(username);

            return GerarSessao(usuario);
        }

        // ETAPA 2 (acessos seguintes): confere o código do app e abre a sessão
        public async Task<TokenDto?> VerificarDoisFatoresAsync(string username, string codigo)
        {
            VerificarBloqueio(username);

            var usuario = await BuscarUsuarioAtivoAsync(username);

            if (string.IsNullOrEmpty(usuario.Chave2FA))
                throw new Exception("O 2FA ainda não foi configurado. Faça login novamente.");

            if (!_twoFactorService.ValidarCodigo(usuario.Chave2FA, codigo, out var passo))
            {
                RegistrarFalha(username);
                return null;
            }

            // Cada código só pode ser usado uma vez (evita reaproveitar um código visto por outra pessoa)
            if (passo <= _cache.Get<long>($"{PASSO_2FA_PREFIX}{username.ToLower()}"))
            {
                RegistrarFalha(username);
                return null;
            }

            MarcarPassoUsado(username, passo);
            LimparFalhas(username);

            return GerarSessao(usuario);
        }

        private async Task<Usuario> BuscarUsuarioAtivoAsync(string username)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Perfil)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (usuario == null || usuario.Status != "Ativo")
                throw new Exception("Usuário não encontrado ou inativo.");

            return usuario;
        }

        // Sessão completa: perfil (permissão) + tempo limitado
        private TokenDto GerarSessao(Usuario usuario)
        {
            var perfil = usuario.Perfil?.Nome ?? "Vendedor";
            var tipo = perfil == "Administrador" ? "Admin" : "PDV";

            return new TokenDto
            {
                AccessToken = _tokenService.GenerateAccessToken(usuario.Username, perfil, tipo),
                ExpiresAt = DateTime.UtcNow.AddMinutes(_tokenService.GetTokenExpirationMinutes()),
                Username = usuario.Username,
                Role = perfil,
                Tipo = tipo
            };
        }

        // ====================== LIMITE DE TENTATIVAS ======================
        // O contador fica na memória da própria API (some se a API reiniciar)

        private void VerificarBloqueio(string username)
        {
            if (_cache.Get<int>($"{FALHAS_PREFIX}{username.ToLower()}") >= MAX_TENTATIVAS)
                throw new TentativasExcedidasException($"Muitas tentativas. Aguarde {BLOQUEIO_MINUTOS} minutos e tente novamente.");
        }

        private void RegistrarFalha(string username)
        {
            var chave = $"{FALHAS_PREFIX}{username.ToLower()}";
            var falhas = _cache.Get<int>(chave) + 1;
            _cache.Set(chave, falhas, TimeSpan.FromMinutes(BLOQUEIO_MINUTOS));
        }

        private void LimparFalhas(string username)
        {
            _cache.Remove($"{FALHAS_PREFIX}{username.ToLower()}");
        }

        private void MarcarPassoUsado(string username, long passo)
        {
            _cache.Set($"{PASSO_2FA_PREFIX}{username.ToLower()}", passo, TimeSpan.FromMinutes(5));
        }
    }
}
