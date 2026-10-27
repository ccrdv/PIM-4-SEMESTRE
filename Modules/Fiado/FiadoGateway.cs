using Microsoft.EntityFrameworkCore;
using SistemaWhiteLabel.API.Data;
using SistemaWhiteLabel.API.Models;
using SistemaWhiteLabel.API.Services.Interfaces;

namespace SistemaWhiteLabel.API.Modules.Fiado
{
    public class FiadoGateway : IFiadoGateway
    {
        private readonly ApplicationDbContext _context;
        private readonly IModuloService _moduloService;
        private readonly IFiadoService _fiadoService;

        public FiadoGateway(ApplicationDbContext context, IModuloService moduloService, IFiadoService fiadoService)
        {
            _context = context;
            _moduloService = moduloService;
            _fiadoService = fiadoService;
        }

        public Task<bool> EstaAtivoAsync()
        {
            return _moduloService.EstaAtivoAsync(FiadoModule.NOME);
        }

        // Só adiciona e salva a linha do fiado: a venda já foi salva (tem IdVenda)
        public async Task RegistrarVendaFiadaAsync(Venda venda)
        {
            if (!await EstaAtivoAsync())
                throw new Exception("Venda a prazo indisponível: o módulo A prazo está desativado.");

            if (venda.IdPessoa == null)
                throw new Exception("Venda a prazo exige um cliente.");

            _context.Fiados.Add(new FiadoVenda
            {
                IdVenda = venda.IdVenda,
                IdPessoa = venda.IdPessoa.Value,
                ValorOriginal = venda.TotalVenda,
                DataHora = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        public async Task<HashSet<int>> VendasFiadasAsync()
        {
            if (!await EstaAtivoAsync())
                return new HashSet<int>();

            var ids = await _context.Fiados.Select(f => f.IdVenda).ToListAsync();
            return ids.ToHashSet();
        }

        public async Task<decimal> TotalAReceberAsync()
        {
            if (!await EstaAtivoAsync())
                return 0;

            var saldos = await _fiadoService.CalcularSaldosAsync();
            return saldos.Where(s => s.Saldo > 0).Sum(s => s.Saldo);
        }

        public async Task<decimal> TotalFiadoHojeAsync()
        {
            if (!await EstaAtivoAsync())
                return 0;

            var hoje = DateTime.Today;
            var amanha = hoje.AddDays(1);

            return await _context.Fiados
                .Where(f => f.DataHora >= hoje && f.DataHora < amanha)
                .SumAsync(f => f.ValorOriginal);
        }

        public async Task<Dictionary<int, decimal>> SaldoPorClienteAsync()
        {
            if (!await EstaAtivoAsync())
                return new Dictionary<int, decimal>();

            var saldos = await _fiadoService.CalcularSaldosAsync();

            return saldos
                .Where(s => s.Saldo > 0)
                .GroupBy(s => s.IdPessoa)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.Saldo));
        }
    }
}
