using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaWhiteLabel.API.Models
{
    public class Produto
    {
        [Key]
        [Column("id_produto")]
        public int IdProduto { get; set; }

        [Column("descricao_produto")]
        [Required]
        [MaxLength(500)]
        public string Descricao { get; set; } = string.Empty;

        [Column("preco_compra_produto")]
        [Precision(12, 2)]
        public decimal PrecoCompra { get; set; }

        [Column("preco_venda_produto")]
        [Precision(12, 2)]
        public decimal PrecoVenda { get; set; }

        [Column("qdte_produto")]
        public int Qtde { get; set; }

        [Column("id_categoria")]
        [ForeignKey("Categoria")]
        public int IdCategoria { get; set; }

        [Column("estoque_minimo_produto")]
        public int EstoqueMinimo { get; set; } = 10;   // alerta de estoque baixo (por produto)

        [Column("id_unidade_medida")]
        public int IdUnidadeMedida { get; set; } = 1;   // 1 = Unidade (UN), cadastrada no script 01

        [ForeignKey("IdCategoria")]
        public virtual Categoria? Categoria { get; set; }
    }
}
