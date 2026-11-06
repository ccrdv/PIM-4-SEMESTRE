namespace SistemaWhiteLabel.API.Models
{
    public class Pessoa
    {
        public int IdPessoa { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string? Telefone { get; set; }   // Oracle grava string vazia como NULL
        public string? Observacao { get; set; }

        // Navegação
        // Cliente = pessoa que NÃO possui registro em Funcionario
        public Funcionario? Funcionario { get; set; }
        public Usuario? Usuario { get; set; }
        public ICollection<Endereco> Enderecos { get; set; } = new List<Endereco>();
    }
}
