using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using SistemaWhiteLabel.API.Models;

namespace SistemaWhiteLabel.API.Modules.Fornecedor
{
    // Um produto da compra (tb_item_compra)
    public class ItemCompra
    {
        public int IdItemCompra { get; set; }
        public int IdCompra { get; set; }
        public int IdProduto { get; set; }
        public int Qdte { get; set; }

        [Precision(12, 2)]
        public decimal PrecoUnitario { get; set; }

        [Precision(12, 2)]
        public decimal ValorDesconto { get; set; }

        [Precision(12, 2)]
        public decimal ValorTotal { get; set; }

        // Navegação
        [ForeignKey("IdCompra")]
        public Compra? Compra { get; set; }

        [ForeignKey("IdProduto")]
        public Produto? Produto { get; set; }
    }
}
