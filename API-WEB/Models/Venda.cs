using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaWhiteLabel.API.Models
{
    public class Venda
    {
        [Key]
        [Column("id_venda")]
        public int IdVenda { get; set; }

        [Column("data_hora_venda")]
        public DateTime DataHora { get; set; } = DateTime.Now;

        [Column("total_venda")]
        [Precision(12, 2)]
        public decimal TotalVenda { get; set; }

        [Column("total_desconto_venda")]
        [Precision(12, 2)]
        public decimal TotalDesconto { get; set; } = 0;

        [Column("status_venda_venda")]
        [MaxLength(15)]
        public string Status { get; set; } = "Concluido";

        [Column("id_pessoa")]
        public int? IdPessoa { get; set; }   // Cliente da venda (null = venda à vista)

        [Column("id_usuario")]
        public int IdUsuario { get; set; }   // Usuário que registrou a venda

        // Navegação
        [ForeignKey("IdPessoa")]
        public Pessoa? Pessoa { get; set; }

        [ForeignKey("IdUsuario")]
        public Usuario? Usuario { get; set; }

        public ICollection<ItemVenda> Itens { get; set; } = new List<ItemVenda>();
        public ICollection<FormaPagamento> Pagamentos { get; set; } = new List<FormaPagamento>();
    }
}
