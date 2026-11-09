namespace SistemaWhiteLabel.API.Modules.Fornecedor
{
    // Pendente, Concluido ou Cancelado (tb_status_compra)
    public class StatusCompra
    {
        public int IdStatusCompra { get; set; }
        public string Descricao { get; set; } = string.Empty;
    }
}
