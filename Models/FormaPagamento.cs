using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaWhiteLabel.API.Models
{
    [Table("tb_forma_pagamento")]
    public class FormaPagamento
    {
        [Key]
        [Column("id_forma_pagamento")]
        public int IdFormaPgto { get; set; }

        [Column("data_hora_pagamento")]
        public DateTime DataHoraPgto { get; set; } = DateTime.Now;

        [Column("total_forma_pagamento")]
        [Precision(12, 2)]
        public decimal? TotalPgto { get; set; }

        [Column("id_venda")]
        public int IdVenda { get; set; }

        [Column("id_tipo_pagamento")]
        public int IdTipoPgto { get; set; }

        // Navegação
        [ForeignKey("IdVenda")]
        public virtual Venda? Venda { get; set; }

        [ForeignKey("IdTipoPgto")]
        public virtual TipoPagamento? TipoPagamento { get; set; }
    }
}
