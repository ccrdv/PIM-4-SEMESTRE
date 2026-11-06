namespace SistemaWhiteLabel.API.DTOs
{
    public class ModuloDto
    {
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public bool Ativo { get; set; }
    }

    public class AlterarModuloDto
    {
        public bool Ativo { get; set; }
    }
}
