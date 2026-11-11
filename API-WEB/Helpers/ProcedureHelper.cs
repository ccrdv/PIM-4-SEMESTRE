using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using SistemaWhiteLabel.API.Data;

namespace SistemaWhiteLabel.API.Helpers
{
    // Chama as procedures do banco (Banco/08_procedures_views.sql) dentro da transação que estiver aberta
    public static class ProcedureHelper
    {
        public const string BAIXAR_ESTOQUE_VENDA = "sp_baixar_estoque_venda";
        public const string RECEBER_COMPRA = "sp_receber_compra";

        public static async Task ExecutarAsync(ApplicationDbContext context, string procedure, int id)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync(
                    "BEGIN \"PIM\".\"" + procedure + "\"(:p_id); END;",
                    new OracleParameter("p_id", id));
            }
            catch (OracleException ex) when (ex.Number >= 20000 && ex.Number <= 20999)
            {
                // Erro de regra levantado pela procedure (RAISE_APPLICATION_ERROR): devolve só a mensagem
                throw new Exception(MensagemDaProcedure(ex.Message));
            }
            catch (OracleException ex) when (ex.Number == 6550 || ex.Number == 4063 || ex.Number == 4068)
            {
                throw new Exception($"A procedure {procedure} não existe ou está inválida no banco. Rode o script Banco/08_procedures_views.sql.");
            }
        }

        // "ORA-20011: Estoque insuficiente! ...\nORA-06512: at ..." -> "Estoque insuficiente! ..."
        private static string MensagemDaProcedure(string mensagem)
        {
            var linha = mensagem.Split('\n')[0].Trim();
            var separador = linha.IndexOf(": ");

            return linha.StartsWith("ORA-") && separador > 0 ? linha.Substring(separador + 2) : linha;
        }
    }
}
