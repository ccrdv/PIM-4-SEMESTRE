using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SistemaWhiteLabel.API.Helpers;
using SistemaWhiteLabel.API.Services.Interfaces;

namespace SistemaWhiteLabel.API.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private const int DOIS_FATORES_EXPIRATION_MINUTES = 5;   // tempo para digitar o código do 2FA

        public TokenService(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        // Duração da sessão (appsettings.json -> JwtSettings:AccessTokenExpirationMinutes)
        public int GetTokenExpirationMinutes()
        {
            return _jwtSettings.AccessTokenExpirationMinutes;
        }

        public int GetTwoFactorExpirationMinutes()
        {
            return DOIS_FATORES_EXPIRATION_MINUTES;
        }

        // Token da sessão completa (só é emitido depois do 2FA)
        public string GenerateAccessToken(string username, string role, string tipo)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim("tipo", tipo),
                new Claim("etapa", "sessao"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            };

            return CriarToken(claims, GetTokenExpirationMinutes());
        }

        // Token temporário da etapa do 2FA: não tem perfil e só vale nos endpoints /Auth/2fa
        public string GenerateTwoFactorToken(string username)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim("etapa", "2fa"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            };

            return CriarToken(claims, DOIS_FATORES_EXPIRATION_MINUTES);
        }

        // Token do celular (autorizado pelo PC por QR Code). Vale até "venceEm" (limite absoluto);
        // a validade real da sessão fica em tb_dispositivo e é conferida a cada requisição (Program.cs).
        public string GenerateDeviceToken(string username, string role, int idDispositivo, DateTime venceEm)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim("tipo", "Mobile"),
                new Claim("etapa", "sessao"),
                new Claim("did", idDispositivo.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            };

            return CriarToken(claims, venceEm.ToUniversalTime());
        }

        private string CriarToken(List<Claim> claims, int expirationMinutes)
        {
            return CriarToken(claims, DateTime.UtcNow.AddMinutes(expirationMinutes));
        }

        private string CriarToken(List<Claim> claims, DateTime venceEmUtc)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = venceEmUtc,
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
