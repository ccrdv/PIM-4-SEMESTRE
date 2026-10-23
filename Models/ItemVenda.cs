using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaWhiteLabel.API.Models
{
    public class ItemVenda
    {
        [Key]
        [Column("Id_Item_Venda")]
        public int IdItemVenda { get; set; }

        [Column("Id_Venda")]
        public int IdVenda { get; set; }

        [Column("Id_Produto")]
        public int IdProduto { get; set; }

        [Column("QDTE_Item_Venda")]
        public int Qdte { get; set; }

        [Column("Preco_Unitario_Item_Venda")]
        [Precision(12, 2)]
        public decimal PrecoUnitario { get; set; }

        [Column("Valor_Desconto_Item_Venda")]
        [Precision(12, 2)]
        public decimal ValorDesconto { get; set; } = 0;

        [Column("Valor_Total_Item_Venda")]
        [Precision(12, 2)]
        public decimal ValorTotal { get; set; }

        // Navegação
        [ForeignKey("IdVenda")]
        public Venda? Venda { get; set; }

        [ForeignKey("IdProduto")]
        public Produto? Produto { get; set; }
    }
}
