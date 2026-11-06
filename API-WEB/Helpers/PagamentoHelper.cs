namespace SistemaWhiteLabel.API.Helpers
{
    public static class PagamentoHelper
    {
        // Texto da forma de pagamento de uma venda: "A prazo", "Pix", "Dinheiro + Cartão"...
        public static string NomeDaForma(IEnumerable<string> tipos, bool isFiado)
        {
            if (isFiado)
                return "A prazo";

            var nomes = tipos.Distinct().ToList();
            return nomes.Any() ? string.Join(" + ", nomes) : "—";
        }
    }
}
