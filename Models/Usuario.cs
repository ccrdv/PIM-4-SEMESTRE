using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaWhiteLabel.API.Models
{
    [Table("tb_usuario")]
    public class Usuario
    {
        [Key]
        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Column("senha")]
        public string Senha { get; set; } = string.Empty;

        [Column("status")]
        public string Status { get; set; } = "Ativo";

        [Column("chave_2fa")]
        public string? Chave2FA { get; set; }

        [Column("id_pessoa")]
        public int IdPessoa { get; set; }

        [Column("id_perfil")]
        public int IdPerfil { get; set; }

        // Navegação
        [ForeignKey("IdPessoa")]
        public virtual Pessoa? Pessoa { get; set; }

        [ForeignKey("IdPerfil")]
        public virtual Perfil? Perfil { get; set; }
    }
}
