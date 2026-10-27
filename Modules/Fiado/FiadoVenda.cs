using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using SistemaWhiteLabel.API.Models;

namespace SistemaWhiteLabel.API.Modules.Fiado
{
    // Uma linha para cada venda feita no fiado (tb_fiado).
    // O saldo não é gravado: valor_original - soma dos pagamentos da venda (tb_forma_pagamento)
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
