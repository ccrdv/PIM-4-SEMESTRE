namespace SistemaWhiteLabel.API.DTOs
{
    public class CategoriaDto
    {
        public int IdCategoria { get; set; }
        public string Descricao { get; set; } = string.Empty;
    }

    public class CategoriaCreateDto
    {
        public string Descricao { get; set; } = string.Empty;
    }
}