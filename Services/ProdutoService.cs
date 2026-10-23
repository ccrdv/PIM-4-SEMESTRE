using SistemaWhiteLabel.API.DTOs;
using SistemaWhiteLabel.API.Models;
using SistemaWhiteLabel.API.Repositories.Interfaces;
using SistemaWhiteLabel.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using SistemaWhiteLabel.API.Data;

namespace SistemaWhiteLabel.API.Services
{
    public class ProdutoService : IProdutoService
    {
        private readonly IProdutoRepository _repository;
        private readonly ApplicationDbContext _context;

        public ProdutoService(IProdutoRepository repository, ApplicationDbContext context)
        {
            _repository = repository;
            _context = context;
        }
        public async Task<IEnumerable<ProdutoDto>> BuscarAsync(string termo)
        {
            var produtos = await _context.Produtos
                .Include(p => p.Categoria)
                .Where(p => p.Descricao.Contains(termo))
                .Take(20)
                .ToListAsync();

            return produtos.Select(p => new ProdutoDto
            {
                IdProduto = p.IdProduto,
                Descricao = p.Descricao,
                PrecoCompra = p.PrecoCompra,
                PrecoVenda = p.PrecoVenda,
                Qtde = p.Qtde,
                IdCategoria = p.IdCategoria,
                IdUnidadeMedida = p.IdUnidadeMedida,
                EstoqueMinimo = p.EstoqueMinimo,
                CategoriaDescricao = p.Categoria?.Descricao
            });
        }

        public async Task<IEnumerable<ProdutoDto>> GetAllAsync()
        {
            var produtos = await _repository.GetAllAsync();
            return produtos.Select(p => new ProdutoDto
            {
                IdProduto = p.IdProduto,
                Descricao = p.Descricao,
                PrecoCompra = p.PrecoCompra,
                PrecoVenda = p.PrecoVenda,
                Qtde = p.Qtde,
                IdCategoria = p.IdCategoria,
                IdUnidadeMedida = p.IdUnidadeMedida,
                EstoqueMinimo = p.EstoqueMinimo,
                CategoriaDescricao = p.Categoria?.Descricao
            });
        }

        public async Task<ProdutoDto?> GetByIdAsync(int id)
        {
            var produto = await _repository.GetByIdAsync(id);
            if (produto == null) return null;

            return new ProdutoDto
            {
                IdProduto = produto.IdProduto,
                Descricao = produto.Descricao,
                PrecoCompra = produto.PrecoCompra,
                PrecoVenda = produto.PrecoVenda,
                Qtde = produto.Qtde,
                IdCategoria = produto.IdCategoria,
                IdUnidadeMedida = produto.IdUnidadeMedida,
                EstoqueMinimo = produto.EstoqueMinimo,
                CategoriaDescricao = produto.Categoria?.Descricao
            };
        }

        public async Task<ProdutoDto> CreateAsync(ProdutoCreateDto dto)
        {
            // Regras de negócio
            ValidarProduto(dto);

            var produto = new Produto
            {
                Descricao = dto.Descricao.Trim(),
                PrecoCompra = dto.PrecoCompra,
                PrecoVenda = dto.PrecoVenda,
                Qtde = dto.Qtde,
                IdCategoria = await ObterCategoriaAsync(dto.IdCategoria),
                IdUnidadeMedida = dto.IdUnidadeMedida ?? 1,
                EstoqueMinimo = dto.EstoqueMinimo ?? 10
            };

            await _repository.AddAsync(produto);

            return await GetByIdAsync(produto.IdProduto) ?? throw new Exception("Erro ao criar produto");
        }

        public async Task UpdateAsync(int id, ProdutoCreateDto dto)
        {
            var produto = await _repository.GetByIdAsync(id);
            if (produto == null)
                throw new Exception("Produto não encontrado.");

            ValidarProduto(dto);

            produto.Descricao = dto.Descricao.Trim();
            produto.PrecoCompra = dto.PrecoCompra;
            produto.PrecoVenda = dto.PrecoVenda;
            produto.Qtde = dto.Qtde;
            produto.IdCategoria = await ObterCategoriaAsync(dto.IdCategoria);

            if (dto.EstoqueMinimo.HasValue)
                produto.EstoqueMinimo = dto.EstoqueMinimo.Value;

            if (dto.IdUnidadeMedida.HasValue)
                produto.IdUnidadeMedida = dto.IdUnidadeMedida.Value;

            await _repository.UpdateAsync(produto);
        }

        public async Task DeleteAsync(int id)
        {
            if (!await _repository.ExistsAsync(id))
                throw new Exception("Produto não encontrado.");

            // O histórico de vendas precisa continuar com o produto
            if (await _context.ItensVenda.AnyAsync(i => i.IdProduto == id))
                throw new Exception("Não é possível excluir: o produto possui vendas registradas.");

            await _repository.DeleteAsync(id);
        }

        private static void ValidarProduto(ProdutoCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Descricao))
                throw new Exception("O nome do produto é obrigatório.");

            if (dto.PrecoVenda <= 0)
                throw new Exception("Preço de venda deve ser maior que zero.");

            if (dto.PrecoCompra < 0)
                throw new Exception("Preço de custo não pode ser negativo.");

            if (dto.Qtde < 0)
                throw new Exception("Quantidade não pode ser negativa.");

            if (dto.EstoqueMinimo.HasValue && dto.EstoqueMinimo.Value < 0)
                throw new Exception("Estoque mínimo não pode ser negativo.");
        }

        // Categoria não informada = "Geral" (cadastrada pelo script 04)
        private async Task<int> ObterCategoriaAsync(int idCategoria)
        {
            if (idCategoria > 0)
            {
                if (!await _context.Categorias.AnyAsync(c => c.IdCategoria == idCategoria))
                    throw new Exception("Categoria não encontrada.");

                return idCategoria;
            }

            var geral = await _context.Categorias
                .FirstOrDefaultAsync(c => c.Descricao.ToUpper() == "GERAL");

            if (geral == null)
                throw new Exception("Categoria 'Geral' não cadastrada. Rode o script 04_ajustes_novo_front.sql.");

            return geral.IdCategoria;
        }
    }
}