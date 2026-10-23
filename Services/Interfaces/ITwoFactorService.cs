namespace SistemaWhiteLabel.API.Services.Interfaces
{
    public interface ITwoFactorService
    {
        string GerarChave();
        string GerarUriOtpAuth(string username, string chave);
        bool ChaveValida(string chave);
        bool ValidarCodigo(string chave, string codigo, out long passoValido);
    }
}
