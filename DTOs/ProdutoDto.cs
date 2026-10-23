namespace SistemaWhiteLabel.API.DTOs
{
    public class ProdutoDto
    {
        public int IdProduto { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public decimal PrecoCompra { get; set; }
        public decimal PrecoVenda { get; set; }
        public int Qtde { get; set; }
        public int IdCategoria { get; set; }
        public int IdUnidadeMedida { get; set; }
        public int EstoqueMinimo { get; set; }
        public string? CategoriaDescricao { get; set; }
    }

    // DTO para criação (sem ID)
    public class ProdutoCreateDto
    {
        public string Descricao { get; set; } = string.Empty;
        public decimal PrecoCompra { get; set; }
        public decimal PrecoVenda { get; set; }
        public int Qtde { get; set; }
        public int IdCategoria { get; set; }
        public int? IdUnidadeMedida { get; set; }   // opcional: se não vier, usa 1 (Unidade)
        public int? EstoqueMinimo { get; set; }     // opcional: se não vier, usa 10
    }
}