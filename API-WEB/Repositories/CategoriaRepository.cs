using Microsoft.EntityFrameworkCore;
using SistemaWhiteLabel.API.Data;
using SistemaWhiteLabel.API.Models;
using SistemaWhiteLabel.API.Repositories.Interfaces;

namespace SistemaWhiteLabel.API.Repositories
{
    public class CategoriaRepository : ICategoriaRepository
    {
        private readonly ApplicationDbContext _context;

        public CategoriaRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Categoria>> GetAllAsync()
            => await _context.Categorias.ToListAsync();

        public async Task<Categoria?> GetByIdAsync(int id)
            => await _context.Categorias.FindAsync(id);

        public async Task AddAsync(Categoria categoria)
        {
            await _context.Categorias.AddAsync(categoria);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Categoria categoria)
        {
            _context.Categorias.Update(categoria);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria != null)
            {
                _context.Categorias.Remove(categoria);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
            => await _context.Categorias.AnyAsync(c => c.IdCategoria == id);
    }
}