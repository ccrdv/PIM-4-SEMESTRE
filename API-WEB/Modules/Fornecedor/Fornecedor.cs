namespace SistemaWhiteLabel.API.Modules.Fornecedor
{
    // Fornecedor = linha de tb_distribuidora (o nome da tabela veio do banco original)
    public class Fornecedor
    {
        public int IdFornecedor { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Cnpj { get; set; } = string.Empty;      // só os 14 dígitos
        public string? Ramo { get; set; }
        public string Status { get; set; } = "Ativo";          // Ativo ou Inativo
        public string? Telefone { get; set; }
        public string? Email { get; set; }
    }
}
