namespace SistemaWhiteLabel.API.Services.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(string username, string role, string tipo);
        string GenerateTwoFactorToken(string username);
        string GenerateDeviceToken(string username, string role, int idDispositivo, DateTime venceEm);
        int GetTokenExpirationMinutes();
        int GetTwoFactorExpirationMinutes();
    }
}
