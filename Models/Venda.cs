using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaWhiteLabel.API.Models
{
    public class Venda
    {
        [Key]
        [Column("Id_Venda")]
        public int IdVenda { get; set; }

        [Column("Data_Hora_Venda")]
        public DateTime DataHora { get; set; } = DateTime.Now;

        [Column("Total_Venda")]
        [Precision(12, 2)]
        public decimal TotalVenda { get; set; }

        [Column("Total_Desconto_Venda")]
        [Precision(12, 2)]
        public decimal TotalDesconto { get; set; } = 0;

        [Column("Status_Venda_Venda")]
        [MaxLength(15)]
        public string Status { get; set; } = "Concluido";

        [Column("Id_Pessoa")]
        public int? IdPessoa { get; set; }   // Cliente da venda (null = venda à vista)

        [Column("Id_Usuario")]
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
