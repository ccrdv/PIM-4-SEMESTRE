using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using SistemaWhiteLabel.API.Models;

namespace SistemaWhiteLabel.API.Modules.Fiado
{
    // Uma linha para cada venda feita no fiado (tbl_Fiado).
    // O saldo não é gravado: Valor_Original - soma dos pagamentos da venda (tbl_Forma_Pagamento)
    public class FiadoVenda
    {
        public int IdFiado { get; set; }
        public int IdVenda { get; set; }
        public int IdPessoa { get; set; }

        [Precision(12, 2)]
        public decimal ValorOriginal { get; set; }

        public DateTime DataHora { get; set; } = DateTime.Now;

        // Navegação
        [ForeignKey("IdVenda")]
        public Venda? Venda { get; set; }

        [ForeignKey("IdPessoa")]
        public Pessoa? Pessoa { get; set; }
    }
}
