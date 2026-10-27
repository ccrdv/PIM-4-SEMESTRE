using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SistemaWhiteLabel.API.Data;
using SistemaWhiteLabel.API.DTOs;
using SistemaWhiteLabel.API.Helpers;
using SistemaWhiteLabel.API.Models;
using SistemaWhiteLabel.API.Services.Interfaces;

namespace SistemaWhiteLabel.API.Services
{
    // Celular autorizado pelo PC: o PC (já logado com 2FA) gera um QR Code, o celular lê e recebe uma sessão
    // de poucos dias. Passado o prazo, o PC renova ou o QR Code é lido de novo.
    public class DispositivoService : IDispositivoService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IMemoryCache _cache;
        private readonly int _diasDeSessao;
        private readonly int _limiteDias;

        private const string PAREAMENTO_PREFIX = "pareamento_";
        private const string FALHAS_PREFIX = "falhas_pareamento_";
        private const int PAREAMENTO_MINUTOS = 2;     // tempo para ler o QR Code
        private const int MAX_TENTATIVAS = 10;
        private const int BLOQUEIO_MINUTOS = 5;

        public DispositivoService(
            ApplicationDbContext context,
            ITokenService tokenService,
            IMemoryCache cache,
            IConfiguration configuration)
        {
            _context = context;
            _tokenService = tokenService;
            _cache = cache;

            // appsettings.json -> Dispositivos
            _diasDeSessao = configuration.GetValue("Dispositivos:DiasDeSessao", 3);
            _limiteDias = configuration.GetValue("Dispositivos:LimiteDias", 30);
        }

        // PC: gera o código que vai dentro do QR Code (vale 2 minutos e só uma vez)
        public PareamentoDto IniciarPareamento(string username)
        {
            var codigo = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');

            var expiraEm = DateTime.Now.AddMinutes(PAREAMENTO_MINUTOS);
            _cache.Set($"{PAREAMENTO_PREFIX}{codigo}", username, TimeSpan.FromMinutes(PAREAMENTO_MINUTOS));

            return new PareamentoDto { Codigo = codigo, ExpiraEm = expiraEm };
        }

        // Celular: troca o código do QR Code por uma sessão
        public async Task<ParearResultadoDto> ParearAsync(string codigo, string? nomeDispositivo, string ip)
        {
            var chaveFalhas = $"{FALHAS_PREFIX}{ip}";

            if (_cache.Get<int>(chaveFalhas) >= MAX_TENTATIVAS)
                throw new TentativasExcedidasException($"Muitas tentativas. Aguarde {BLOQUEIO_MINUTOS} minutos e tente novamente.");

            var chaveCodigo = $"{PAREAMENTO_PREFIX}{codigo}";

            if (string.IsNullOrWhiteSpace(codigo) || !_cache.TryGetValue(chaveCodigo, out string? username) || username == null)
            {
                _cache.Set(chaveFalhas, _cache.Get<int>(chaveFalhas) + 1, TimeSpan.FromMinutes(BLOQUEIO_MINUTOS));
                throw new Exception("QR Code inválido ou expirado. Gere outro no computador.");
            }

            // Cada código só pode ser usado uma vez
            _cache.Remove(chaveCodigo);

            var usuario = await _context.Usuarios
                .Include(u => u.Perfil)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (usuario == null || usuario.Status != "Ativo")
                throw new Exception("Usuário não encontrado ou inativo.");

            var agora = DateTime.Now;
            var nome = string.IsNullOrWhiteSpace(nomeDispositivo) ? "Celular" : nomeDispositivo.Trim();

            var dispositivo = new Dispositivo
            {
                IdUsuario = usuario.IdUsuario,
                Nome = nome.Length > 100 ? nome.Substring(0, 100) : nome,
                CriadoEm = agora,
                ExpiraEm = agora.AddDays(_diasDeSessao),
                LimiteEm = agora.AddDays(_limiteDias)
            };

            _context.Dispositivos.Add(dispositivo);
            await _context.SaveChangesAsync();   // gera Id_Dispositivo

            var perfil = usuario.Perfil?.Nome ?? "Vendedor";

            return new ParearResultadoDto
            {
                AccessToken = _tokenService.GenerateDeviceToken(usuario.Username, perfil, dispositivo.IdDispositivo, dispositivo.LimiteEm),
                IdDispositivo = dispositivo.IdDispositivo,
                Username = usuario.Username,
                Role = perfil,
                ExpiraEm = dispositivo.ExpiraEm,
                LimiteEm = dispositivo.LimiteEm
            };
        }

        // PC: aparelhos do usuário (os desconectados saem da lista)
        public async Task<IEnumerable<DispositivoResponseDto>> ListarAsync(string username)
        {
            var agora = DateTime.Now;

            var lista = await _context.Dispositivos
                .Where(d => d.Usuario!.Username == username && d.RevogadoEm == null)
                .OrderByDescending(d => d.CriadoEm)
                .ToListAsync();

            return lista.Select(d => MapToResponse(d, agora)).ToList();
        }

        // PC: estende a sessão do celular por mais alguns dias (nunca além do limite)
        public async Task<DispositivoResponseDto?> RenovarAsync(int id, string username)
        {
            var agora = DateTime.Now;

            var dispositivo = await _context.Dispositivos
                .FirstOrDefaultAsync(d => d.IdDispositivo == id && d.Usuario!.Username == username && d.RevogadoEm == null);

            if (dispositivo == null)
                return null;

            if (dispositivo.LimiteEm <= agora)
                throw new Exception("Este aparelho chegou ao limite de renovações. Leia o QR Code de novo no celular.");

            var novoFim = agora.AddDays(_diasDeSessao);
            dispositivo.ExpiraEm = novoFim < dispositivo.LimiteEm ? novoFim : dispositivo.LimiteEm;

            await _context.SaveChangesAsync();
            return MapToResponse(dispositivo, agora);
        }

        // PC: desconecta o celular na hora (a próxima requisição dele recebe 401)
        public async Task<bool> RevogarAsync(int id, string username)
        {
            var dispositivo = await _context.Dispositivos
                .FirstOrDefaultAsync(d => d.IdDispositivo == id && d.Usuario!.Username == username && d.RevogadoEm == null);

            if (dispositivo == null)
                return false;

            dispositivo.RevogadoEm = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        // Celular: quanto tempo ainda tem
        public async Task<MinhaSessaoDto?> MinhaSessaoAsync(int idDispositivo)
        {
            var d = await _context.Dispositivos.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdDispositivo == idDispositivo);

            if (d == null)
                return null;

            return new MinhaSessaoDto
            {
                IdDispositivo = d.IdDispositivo,
                Nome = d.Nome,
                ExpiraEm = d.ExpiraEm,
                LimiteEm = d.LimiteEm
            };
        }

        private static DispositivoResponseDto MapToResponse(Dispositivo d, DateTime agora)
        {
            return new DispositivoResponseDto
            {
                IdDispositivo = d.IdDispositivo,
                Nome = d.Nome,
                CriadoEm = d.CriadoEm,
                ExpiraEm = d.ExpiraEm,
                LimiteEm = d.LimiteEm,
                UltimoUso = d.UltimoUso,
                Ativo = d.RevogadoEm == null && d.ExpiraEm > agora
            };
        }
    }
}
