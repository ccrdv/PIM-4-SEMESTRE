using SistemaWhiteLabel.API.DTOs;
using SistemaWhiteLabel.API.Models;
using SistemaWhiteLabel.API.Repositories.Interfaces;
using SistemaWhiteLabel.API.Services.Interfaces;
using SistemaWhiteLabel.API.Data;
using Microsoft.EntityFrameworkCore;

namespace SistemaWhiteLabel.API.Services
{
    public class CategoriaService : ICategoriaService
    {
        private readonly ICategoriaRepository _repository;
        private readonly ApplicationDbContext _context;

        public CategoriaService(ICategoriaRepository repository, ApplicationDbContext context)
        {
            _repository = repository;
            _context = context;
        }

        public async Task<IEnumerable<CategoriaDto>> GetAllAsync()
        {
            var categorias = await _repository.GetAllAsync();
            return categorias.Select(c => new CategoriaDto
            {
                IdCategoria = c.IdCategoria,
                Descricao = c.Descricao
            });
        }

        public async Task<CategoriaDto?> GetByIdAsync(int id)
        {
            var categoria = await _repository.GetByIdAsync(id);
            if (categoria == null) return null;

            return new CategoriaDto
            {
                IdCategoria = categoria.IdCategoria,
                Descricao = categoria.Descricao
            };
        }

        public async Task<CategoriaDto> CreateAsync(CategoriaCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Descricao))
                throw new Exception("Descrição da categoria é obrigatória.");

            var categoria = new Categoria
            {
                Descricao = dto.Descricao.Trim()
            };

            await _repository.AddAsync(categoria);
            return await GetByIdAsync(categoria.IdCategoria)
                ?? throw new Exception("Erro ao criar categoria.");
        }

        public async Task UpdateAsync(int id, CategoriaCreateDto dto)
        {
            var categoria = await _repository.GetByIdAsync(id);
            if (categoria == null)
                throw new Exception("Categoria não encontrada.");

            if (string.IsNullOrWhiteSpace(dto.Descricao))
                throw new Exception("Descrição da categoria é obrigatória.");

            categoria.Descricao = dto.Descricao.Trim();
            await _repository.UpdateAsync(categoria);
        }

        public async Task DeleteAsync(int id)
        {
            var categoria = await _repository.GetByIdAsync(id);
            if (categoria == null)
                throw new Exception("Categoria não encontrada.");

            // "Geral" é a categoria padrão dos produtos novos
            if (categoria.Descricao.Trim().ToUpper() == "GERAL")
                throw new Exception("A categoria 'Geral' é a padrão e não pode ser excluída.");

            var emUso = await _context.Produtos.CountAsync(p => p.IdCategoria == id);
            if (emUso > 0)
                throw new Exception($"Não é possível excluir: {emUso} produto(s) usam esta categoria.");

            await _repository.DeleteAsync(id);
        }
    }
}