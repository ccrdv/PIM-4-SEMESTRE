namespace SistemaWhiteLabel.API.Models
{
    // Uma linha da view sv_vendas_relatorio_fechamento: o fechamento de um dia (só leitura)
    public class FechamentoDia
    {
        public DateTime Data { get; set; }

        public int QtdeVendas { get; set; }

        // Bruto = sem desconto; líquido = com desconto
        public decimal TotalBruto { get; set; }

        public decimal TotalDescontos { get; set; }

        public decimal TotalLiquido { get; set; }

        public decimal TotalDinheiro { get; set; }

        public decimal TotalPix { get; set; }

        public decimal TotalCartao { get; set; }
    }
}
