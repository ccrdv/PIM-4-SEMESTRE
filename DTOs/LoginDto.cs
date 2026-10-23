namespace SistemaWhiteLabel.API.DTOs
{
    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }

    // Resposta da etapa 1 do login (usuário e senha corretos)
    public class LoginEtapa1Dto
    {
        public string TokenTemporario { get; set; } = string.Empty;   // bearer da etapa do 2FA (vale poucos minutos)
        public DateTime ExpiraEm { get; set; }
        public bool DoisFatoresConfigurado { get; set; }
        public string Username { get; set; } = string.Empty;
    }

    public class ConfigurarDoisFatoresDto
    {
        public string Chave { get; set; } = string.Empty;
        public string UriOtpAuth { get; set; } = string.Empty;
    }

    public class AtivarDoisFatoresDto
    {
        public string Chave { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
    }

    public class VerificarDoisFatoresDto
    {
        public string Codigo { get; set; } = string.Empty;
    }

    // Sessão completa (depois do 2FA)
    public class TokenDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
    }
}
