namespace SistemaWhiteLabel.API.Services.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(string username, string role, string tipo);
        string GenerateTwoFactorToken(string username);
        int GetTokenExpirationMinutes();
        int GetTwoFactorExpirationMinutes();
    }
}
