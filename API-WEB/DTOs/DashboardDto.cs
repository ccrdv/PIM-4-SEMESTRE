namespace SistemaWhiteLabel.API.DTOs
{
    // Resumo de um período. Líquido = com o desconto já abatido; Bruto = preço cheio, sem desconto.
    public class DashboardDto
    {
        public string Periodo { get; set; } = string.Empty;
        public DateTime De { get; set; }
        public DateTime Ate { get; set; }

        public int QuantidadeVendas { get; set; }
        public int ProdutosVendidos { get; set; }              // unidades
        public decimal FaturamentoLiquido { get; set; }
        public decimal FaturamentoBruto { get; set; }
        public decimal TotalDescontos { get; set; }
        public decimal TicketMedioLiquido { get; set; }
        public decimal TicketMedioBruto { get; set; }

        public int ProdutosSemEstoque { get; set; }
        public int ProdutosBaixoEstoque { get; set; }          // abaixo do mínimo de cada produto (e ainda não zerados)

        public bool FiadoAtivo { get; set; }
        public decimal TotalFiado { get; set; }                // a receber (todos os períodos)
        public decimal VendasFiadasPeriodo { get; set; }

        public List<DashboardDiaDto> VendasPorDia { get; set; } = new();
        public List<DashboardProdutoDto> PorProduto { get; set; } = new();
        public List<DashboardCategoriaDto> PorCategoria { get; set; } = new();
        public List<DashboardPagamentoDto> PorPagamento { get; set; } = new();
    }

    public class DashboardDiaDto
    {
        public DateTime Data { get; set; }
        public int Quantidade { get; set; }
        public decimal Liquido { get; set; }
        public decimal Bruto { get; set; }
    }

    public class DashboardProdutoDto
    {
        public int IdProduto { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal Liquido { get; set; }
        public decimal Bruto { get; set; }
    }

    public class DashboardCategoriaDto
    {
        public string Nome { get; set; } = string.Empty;
        public decimal Liquido { get; set; }
        public decimal Bruto { get; set; }
    }

    public class DashboardPagamentoDto
    {
        public string Forma { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal Liquido { get; set; }
        public decimal Bruto { get; set; }
    }
}
