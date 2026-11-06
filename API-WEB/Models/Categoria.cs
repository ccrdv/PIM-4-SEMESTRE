namespace SistemaWhiteLabel.API.Models
{
    public class Categoria
    {
        public int IdCategoria { get; set; }           // Chave Primária
        public string Descricao { get; set; } = string.Empty;

        // Relacionamento
        public ICollection<Produto> Produtos { get; set; } = new List<Produto>();
    }
}