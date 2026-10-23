namespace SistemaWhiteLabel.API.Models
{
    public class Modulo
    {
        public string Nome { get; set; } = string.Empty;       // ex.: "fiado"
        public string? Descricao { get; set; }
        public string Ativo { get; set; } = "S";                // "S" ligado, "N" desligado
    }
}
