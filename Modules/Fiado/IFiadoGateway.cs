using SistemaWhiteLabel.API.Models;

namespace SistemaWhiteLabel.API.Modules.Fiado
{
    // Porta de entrada do módulo para o resto do sistema (vendas, dashboard, clientes).
    // Com o módulo desligado, tudo aqui devolve zero e o registro de venda fiada é recusado.
    public interface IFiadoGateway
    {
        Task<bool> EstaAtivoAsync();
        Task RegistrarVendaFiadaAsync(Venda venda);               // chamar dentro da transação da venda
        Task<HashSet<int>> VendasFiadasAsync();                   // Ids das vendas feitas no fiado
        Task<decimal> TotalAReceberAsync();
        Task<decimal> TotalFiadoHojeAsync();
        Task<Dictionary<int, decimal>> SaldoPorClienteAsync();    // IdPessoa -> saldo devedor
    }
}
