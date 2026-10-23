namespace SistemaWhiteLabel.API.Models
{
    public class Funcionario
    {
        public int IdPessoa { get; set; }   // Chave primária e estrangeira (1:1 com Pessoa)
        public string Cargo { get; set; } = "Vendedor";
        public decimal Salario { get; set; } = 0;

        public Pessoa? Pessoa { get; set; }
    }
}
