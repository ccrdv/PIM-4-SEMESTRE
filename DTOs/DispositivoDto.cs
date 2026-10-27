namespace SistemaWhiteLabel.API.DTOs
{
    // Resposta do PC ao pedir um QR Code
    public class PareamentoDto
    {
        public string Codigo { get; set; } = string.Empty;    // vai dentro do QR Code
        public DateTime ExpiraEm { get; set; }                // o código vale poucos minutos
    }

    // O celular manda o código lido no QR Code
    public class ParearDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string? NomeDispositivo { get; set; }          // ex.: "Pixel 7 da Maria"
    }

    // Sessão entregue ao celular
    public class ParearResultadoDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public int IdDispositivo { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime ExpiraEm { get; set; }
        public DateTime LimiteEm { get; set; }
    }

    // Lista de aparelhos na tela do PC
    public class DispositivoResponseDto
    {
        public int IdDispositivo { get; set; }
        public string Nome { get; set; } = string.Empty;
        public DateTime CriadoEm { get; set; }
        public DateTime ExpiraEm { get; set; }
        public DateTime LimiteEm { get; set; }
        public DateTime? UltimoUso { get; set; }
        public bool Ativo { get; set; }                       // não revogado e não vencido
    }

    // O celular consulta quanto tempo ainda tem
    public class MinhaSessaoDto
    {
        public int IdDispositivo { get; set; }
        public string Nome { get; set; } = string.Empty;
        public DateTime ExpiraEm { get; set; }
        public DateTime LimiteEm { get; set; }
    }
}
