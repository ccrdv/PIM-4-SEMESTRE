using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaWhiteLabel.API.Models
{
    public class Produto
    {
        [Key]
        [Column("Id_Produto")]
        public int IdProduto { get; set; }

        [Column("Descricao_Produto")]
        [Required]
        [MaxLength(500)]
        public string Descricao { get; set; } = string.Empty;

        [Column("Preco_Compra_Produto")]
        [Precision(12, 2)]
        public decimal PrecoCompra { get; set; }

        [Column("Preco_Venda_Produto")]
        [Precision(12, 2)]
        public decimal PrecoVenda { get; set; }

        [Column("QDTE_Produto")]
        public int Qtde { get; set; }

        [Column("Id_Categoria")]
        [ForeignKey("Categoria")]
        public int IdCategoria { get; set; }

        [Column("Estoque_Minimo_Produto")]
        public int EstoqueMinimo { get; set; } = 10;   // alerta de estoque baixo (por produto)

        [Column("Id_Unidade_Medida")]
        public int IdUnidadeMedida { get; set; } = 1;   // 1 = Unidade (UN), cadastrada no script 01

        [ForeignKey("IdCategoria")]
        public virtual Categoria? Categoria { get; set; }
    }
}
