using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaWhiteLabel.API.Models
{
    public class Endereco
    {
        [Key]
        [Column("Id_Endereco")]
        public int IdEndereco { get; set; }

        [Column("Logradouro")]
        [MaxLength(150)]
        public string Logradouro { get; set; } = string.Empty;

        [Column("Numero")]
        public int? Numero { get; set; }

        [Column("Bairro")]
        [MaxLength(100)]
        public string? Bairro { get; set; }

        [Column("CEP")]
        [MaxLength(9)]
        public string? Cep { get; set; }

        [Column("Id_Pessoa")]
        public int IdPessoa { get; set; }

        [Column("Id_Municipio")]
        public int? IdMunicipio { get; set; }

        // Navegação
        [ForeignKey("IdPessoa")]
        public Pessoa? Pessoa { get; set; }
    }
}
