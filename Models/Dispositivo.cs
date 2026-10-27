using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaWhiteLabel.API.Models
{
    // Um celular autorizado pelo PC (QR Code). A sessão do celular vale até ExpiraEm;
    // o PC pode renovar (até LimiteEm) ou desconectar (RevogadoEm).
    public class Dispositivo
    {
        public int IdDispositivo { get; set; }
        public int IdUsuario { get; set; }
        public string Nome { get; set; } = string.Empty;

        public DateTime CriadoEm { get; set; } = DateTime.Now;
        public DateTime ExpiraEm { get; set; }          // fim da sessão atual (3 dias, renovável)
        public DateTime LimiteEm { get; set; }          // depois disso só lendo o QR Code de novo
        public DateTime? UltimoUso { get; set; }
        public DateTime? RevogadoEm { get; set; }

        // Navegação
        [ForeignKey("IdUsuario")]
        public Usuario? Usuario { get; set; }
    }
}
