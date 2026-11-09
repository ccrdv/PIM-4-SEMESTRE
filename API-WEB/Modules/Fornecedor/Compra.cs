using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using SistemaWhiteLabel.API.Models;

namespace SistemaWhiteLabel.API.Modules.Fornecedor
{
    // Uma compra de mercadoria feita a um fornecedor (tb_compra).
    // Pendente = pedido feito, o estoque ainda não mudou. Concluido = mercadoria recebida (estoque somado).
    public class Compra
    {
        public int IdCompra { get; set; }
        public DateTime DataHora { get; set; } = DateTime.Now;

        [Precision(12, 2)]
        public decimal TotalCompra { get; set; }          // líquido (já com desconto)

        [Precision(12, 2)]
        public decimal TotalDesconto { get; set; }

        public int IdUsuario { get; set; }                // quem registrou
        public int IdFornecedor { get; set; }
        public int IdStatusCompra { get; set; }

        // Navegação
        [ForeignKey("IdUsuario")]
        public Usuario? Usuario { get; set; }

        [ForeignKey("IdFornecedor")]
        public Fornecedor? Fornecedor { get; set; }

        [ForeignKey("IdStatusCompra")]
        public StatusCompra? StatusCompra { get; set; }

        public List<ItemCompra> Itens { get; set; } = new();
    }
}
