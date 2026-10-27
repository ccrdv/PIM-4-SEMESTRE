using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SistemaWhiteLabel.API.Data;
using SistemaWhiteLabel.API.DTOs;
using SistemaWhiteLabel.API.Models;
using SistemaWhiteLabel.API.Services.Interfaces;

namespace SistemaWhiteLabel.API.Services
{
    public class ModuloService : IModuloService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ModuloService> _logger;

        private const string CACHE_PREFIX = "modulo:";
        private static readonly TimeSpan CACHE_TEMPO = TimeSpan.FromSeconds(30);   // evita consultar o banco a cada requisição

        public ModuloService(ApplicationDbContext context, IMemoryCache cache, ILogger<ModuloService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        // Módulo que não está cadastrado em tb_modulo é considerado desligado
        public async Task<bool> EstaAtivoAsync(string nome)
        {
            var chave = $"{CACHE_PREFIX}{nome.ToLower()}";

            if (_cache.TryGetValue(chave, out bool ativo))
                return ativo;

            try
            {
                var modulo = await _context.Modulos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Nome == nome.ToLower());

                ativo = modulo != null && modulo.Ativo == "S";
                _cache.Set(chave, ativo, CACHE_TEMPO);

                return ativo;
            }
            catch (Exception ex)
            {
                // Ex.: tb_modulo ainda não foi criada (Banco/03_modulo_fiado.sql). O resto do sistema continua funcionando.
                _logger.LogWarning(ex, "Não foi possível ler tb_modulo; módulo '{Modulo}' tratado como desligado.", nome);
                return false;
            }
        }

        public async Task<IEnumerable<ModuloDto>> ListarAsync()
        {
            List<Modulo> modulos;

            try
            {
                modulos = await _context.Modulos
                    .AsNoTracking()
                    .OrderBy(m => m.Nome)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Não foi possível ler tb_modulo.");
                return new List<ModuloDto>();
            }

            return modulos.Select(m => new ModuloDto
            {
                Nome = m.Nome,
                Descricao = m.Descricao,
                Ativo = m.Ativo == "S"
            });
        }

        public async Task<ModuloDto?> AlterarAsync(string nome, bool ativo)
        {
            var modulo = await _context.Modulos
                .FirstOrDefaultAsync(m => m.Nome == nome.ToLower());

            if (modulo == null)
                return null;

            modulo.Ativo = ativo ? "S" : "N";
            await _context.SaveChangesAsync();

            // O novo estado vale na hora
            _cache.Remove($"{CACHE_PREFIX}{nome.ToLower()}");

            return new ModuloDto
            {
                Nome = modulo.Nome,
                Descricao = modulo.Descricao,
                Ativo = ativo
            };
        }
    }
}
