using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaWhiteLabel.API.Data;

namespace SistemaWhiteLabel.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador,Gerente")]
    public class EstoqueController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EstoqueController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Resumo para os cards do estoque
        /// </summary>
        [HttpGet("resumo")]
        public async Task<IActionResult> GetResumo()
        {
            var totalProdutos = await _context.Produtos.CountAsync();
            // Baixo estoque = quantidade menor ou igual ao estoque mínimo do próprio produto (e ainda não zerado)
            var produtosBaixoEstoque = await _context.Produtos.CountAsync(p => p.Qtde > 0 && p.Qtde <= p.EstoqueMinimo);
            var produtosSemEstoque = await _context.Produtos.CountAsync(p => p.Qtde == 0);
            var valorTotalEstoque = await _context.Produtos.SumAsync(p => p.PrecoVenda * p.Qtde);
            var valorTotalCusto = await _context.Produtos.SumAsync(p => p.PrecoCompra * p.Qtde);

            return Ok(new
            {
                TotalProdutos = totalProdutos,
                ProdutosBaixoEstoque = produtosBaixoEstoque,
                ProdutosSemEstoque = produtosSemEstoque,
                ValorTotalEstoque = valorTotalEstoque,
                ValorTotalCusto = valorTotalCusto
            });
        }

        /// <summary>
        /// Lista produtos zerados ou abaixo do estoque mínimo de cada um
        /// </summary>
        [HttpGet("baixo-estoque")]
        public async Task<IActionResult> GetBaixoEstoque()
        {
            var produtos = await _context.Produtos
                .Include(p => p.Categoria)
                .Where(p => p.Qtde <= p.EstoqueMinimo)
                .OrderBy(p => p.Qtde)
                .Select(p => new
                {
                    p.IdProduto,
                    p.Descricao,
                    p.Qtde,
                    p.EstoqueMinimo,
                    Categoria = p.Categoria != null ? p.Categoria.Descricao : "Sem categoria",
                    p.PrecoVenda,
                    ValorTotal = p.PrecoVenda * p.Qtde
                })
                .ToListAsync();

            return Ok(produtos);
        }

        /// <summary>
        /// Lista todos os produtos com filtro por categoria
        /// </summary>
        [HttpGet("produtos")]
        public async Task<IActionResult> GetAllProdutos([FromQuery] int? categoriaId = null)
        {
            var query = _context.Produtos
                .Include(p => p.Categoria)
                .AsQueryable();

            if (categoriaId.HasValue)
                query = query.Where(p => p.IdCategoria == categoriaId.Value);

            var produtos = await query
                .Select(p => new
                {
                    p.IdProduto,
                    p.Descricao,
                    p.Qtde,
                    p.EstoqueMinimo,
                    p.IdCategoria,
                    Categoria = p.Categoria != null ? p.Categoria.Descricao : "Sem categoria",
                    p.PrecoVenda,
                    p.PrecoCompra,
                    Status = p.Qtde == 0 ? "Sem estoque" : (p.Qtde <= p.EstoqueMinimo ? "Baixo Estoque" : "Normal"),
                    ValorTotalEstoque = p.PrecoVenda * p.Qtde
                })
                .ToListAsync();

            return Ok(produtos);
        }
    }
}