using System.Security.Cryptography;
using System.Text;
using SistemaWhiteLabel.API.Services.Interfaces;

namespace SistemaWhiteLabel.API.Services
{
    // Código de 6 dígitos que muda a cada 30 segundos (TOTP, RFC 6238).
    // Funciona com Microsoft Authenticator, Google Authenticator, Authy etc.
    public class TwoFactorService : ITwoFactorService
    {
        private const string EMISSOR = "SistemaWhiteLabel";
        private const int TAMANHO_CHAVE_BYTES = 20;      // 160 bits
        private const int DIGITOS = 6;
        private const int PERIODO_SEGUNDOS = 30;
        private const int JANELA = 1;                    // aceita 1 passo antes/depois (relógio do celular defasado)
        private const string ALFABETO_BASE32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public string GerarChave()
        {
            var bytes = RandomNumberGenerator.GetBytes(TAMANHO_CHAVE_BYTES);
            return Base32Codificar(bytes);
        }

        // Endereço que o QR Code carrega (o app autenticador lê isso e cadastra a conta)
        public string GerarUriOtpAuth(string username, string chave)
        {
            var emissor = Uri.EscapeDataString(EMISSOR);
            var conta = Uri.EscapeDataString(username);

            return $"otpauth://totp/{emissor}:{conta}?secret={chave}&issuer={emissor}&algorithm=SHA1&digits={DIGITOS}&period={PERIODO_SEGUNDOS}";
        }

        public bool ChaveValida(string chave)
        {
            var bytes = Base32Decodificar(chave);
            return bytes != null && bytes.Length == TAMANHO_CHAVE_BYTES;
        }

        public bool ValidarCodigo(string chave, string codigo, out long passoValido)
        {
            passoValido = 0;

            if (string.IsNullOrWhiteSpace(codigo))
                return false;

            codigo = codigo.Replace(" ", "");   // alguns apps mostram "123 456"

            if (codigo.Length != DIGITOS || !codigo.All(char.IsDigit))
                return false;

            var chaveBytes = Base32Decodificar(chave);
            if (chaveBytes == null)
                return false;

            var passoAtual = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / PERIODO_SEGUNDOS;

            for (long passo = passoAtual - JANELA; passo <= passoAtual + JANELA; passo++)
            {
                var esperado = CalcularCodigo(chaveBytes, passo);

                if (CryptographicOperations.FixedTimeEquals(
                        Encoding.ASCII.GetBytes(esperado),
                        Encoding.ASCII.GetBytes(codigo)))
                {
                    passoValido = passo;
                    return true;
                }
            }

            return false;
        }

        private static string CalcularCodigo(byte[] chave, long passo)
        {
            var mensagem = BitConverter.GetBytes(passo);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(mensagem);

            using var hmac = new HMACSHA1(chave);
            var hash = hmac.ComputeHash(mensagem);

            var offset = hash[hash.Length - 1] & 0x0F;
            var binario = ((hash[offset] & 0x7F) << 24)
                        | (hash[offset + 1] << 16)
                        | (hash[offset + 2] << 8)
                        | hash[offset + 3];

            var codigo = binario % (int)Math.Pow(10, DIGITOS);
            return codigo.ToString().PadLeft(DIGITOS, '0');
        }

        private static string Base32Codificar(byte[] dados)
        {
            var resultado = new StringBuilder();
            int buffer = 0;
            int bitsNoBuffer = 0;

            foreach (var b in dados)
            {
                buffer = (buffer << 8) | b;
                bitsNoBuffer += 8;

                while (bitsNoBuffer >= 5)
                {
                    resultado.Append(ALFABETO_BASE32[(buffer >> (bitsNoBuffer - 5)) & 31]);
                    bitsNoBuffer -= 5;
                    buffer &= (1 << bitsNoBuffer) - 1;
                }
            }

            if (bitsNoBuffer > 0)
                resultado.Append(ALFABETO_BASE32[(buffer << (5 - bitsNoBuffer)) & 31]);

            return resultado.ToString();
        }

        private static byte[]? Base32Decodificar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return null;

            texto = texto.Replace(" ", "").Replace("-", "").TrimEnd('=').ToUpperInvariant();

            var bytes = new List<byte>();
            int buffer = 0;
            int bitsNoBuffer = 0;

            foreach (var c in texto)
            {
                var valor = ALFABETO_BASE32.IndexOf(c);
                if (valor < 0)
                    return null;

                buffer = (buffer << 5) | valor;
                bitsNoBuffer += 5;

                if (bitsNoBuffer >= 8)
                {
                    bytes.Add((byte)((buffer >> (bitsNoBuffer - 8)) & 0xFF));
                    bitsNoBuffer -= 8;
                    buffer &= (1 << bitsNoBuffer) - 1;
                }
            }

            return bytes.ToArray();
        }
    }
}
