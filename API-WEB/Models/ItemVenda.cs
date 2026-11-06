using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaWhiteLabel.API.Models
{
    public class ItemVenda
    {
        [Key]
        [Column("id_item_venda")]
        public int IdItemVenda { get; set; }

        [Column("id_venda")]
        public int IdVenda { get; set; }

        [Column("id_produto")]
        public int IdProduto { get; set; }

        [Column("qdte_item_venda")]
        public int Qdte { get; set; }

        [Column("preco_unitario_item_venda")]
        [Precision(12, 2)]
        public decimal PrecoUnitario { get; set; }

        [Column("valor_desconto_item_venda")]
        [Precision(12, 2)]
        public decimal ValorDesconto { get; set; } = 0;

        [Column("valor_total_item_venda")]
        [Precision(12, 2)]
        public decimal ValorTotal { get; set; }

        // Navegação
        [ForeignKey("IdVenda")]
        public Venda? Venda { get; set; }

        [ForeignKey("IdProduto")]
        public Produto? Produto { get; set; }
    }
}
