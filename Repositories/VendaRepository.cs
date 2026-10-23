using Microsoft.EntityFrameworkCore;
using SistemaWhiteLabel.API.Data;
using SistemaWhiteLabel.API.Models;
using SistemaWhiteLabel.API.Repositories.Interfaces;

namespace SistemaWhiteLabel.API.Repositories
{
    public class VendaRepository : IVendaRepository
    {
        private readonly ApplicationDbContext _context;

        public VendaRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Venda> AddAsync(Venda venda)
        {
            await _context.Vendas.AddAsync(venda);
            await _context.SaveChangesAsync();
            return venda;
        }

        public async Task<Venda?> GetByIdAsync(int id)
        {
            return await _context.Vendas
                .Include(v => v.Itens)
                .Include(v => v.Pessoa)
                .FirstOrDefaultAsync(v => v.IdVenda == id);
        }

        public async Task<IEnumerable<Venda>> GetAllAsync()
        {
            return await _context.Vendas
                .Include(v => v.Itens)
                .Include(v => v.Pessoa)
                .OrderByDescending(v => v.DataHora)
                .ToListAsync();
        }
    }
}