using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaWhiteLabel.API.Models
{
    public class Endereco
    {
        [Key]
        [Column("id_endereco")]
        public int IdEndereco { get; set; }

        [Column("logradouro")]
        [MaxLength(150)]
        public string Logradouro { get; set; } = string.Empty;

        [Column("numero")]
        public int? Numero { get; set; }

        [Column("bairro")]
        [MaxLength(100)]
        public string? Bairro { get; set; }

        [Column("cep")]
        [MaxLength(9)]
        public string? Cep { get; set; }

        [Column("id_pessoa")]
        public int IdPessoa { get; set; }

        [Column("id_municipio")]
        public int? IdMunicipio { get; set; }

        // Navegação
        [ForeignKey("IdPessoa")]
        public Pessoa? Pessoa { get; set; }
    }
}
