using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaWhiteLabel.API.Models
{
    [Table("tbl_Forma_Pagamento")]
    public class FormaPagamento
    {
        [Key]
        [Column("Id_Forma_Pagamento")]
        public int IdFormaPgto { get; set; }

        [Column("Data_Hora_Pagamento")]
        public DateTime DataHoraPgto { get; set; } = DateTime.Now;

        [Column("Total_Forma_Pagamento")]
        [Precision(12, 2)]
        public decimal? TotalPgto { get; set; }

        [Column("Id_Venda")]
        public int IdVenda { get; set; }

        [Column("Id_Tipo_Pagamento")]
        public int IdTipoPgto { get; set; }

        // Navegação
        [ForeignKey("IdVenda")]
        public virtual Venda? Venda { get; set; }

        [ForeignKey("IdTipoPgto")]
        public virtual TipoPagamento? TipoPagamento { get; set; }
    }
}
