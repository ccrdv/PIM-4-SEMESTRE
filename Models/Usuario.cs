using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaWhiteLabel.API.Models
{
    [Table("tbl_Usuario")]
    public class Usuario
    {
        [Key]
        [Column("Id_Usuario")]
        public int IdUsuario { get; set; }

        [Column("UserName")]
        public string Username { get; set; } = string.Empty;

        [Column("Senha")]
        public string Senha { get; set; } = string.Empty;

        [Column("Status")]
        public string Status { get; set; } = "Ativo";

        [Column("Chave_2FA")]
        public string? Chave2FA { get; set; }

        [Column("Id_Pessoa")]
        public int IdPessoa { get; set; }

        [Column("Id_Perfil")]
        public int IdPerfil { get; set; }

        // Navegação
        [ForeignKey("IdPessoa")]
        public virtual Pessoa? Pessoa { get; set; }

        [ForeignKey("IdPerfil")]
        public virtual Perfil? Perfil { get; set; }
    }
}
