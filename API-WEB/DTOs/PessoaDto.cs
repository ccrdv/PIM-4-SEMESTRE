namespace SistemaWhiteLabel.API.DTOs
{
    public class PessoaCreateDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Endereco { get; set; } = string.Empty;
        public string? Observacao { get; set; }

        // Flags para definir o tipo
        public bool IsCliente { get; set; } = false;
        public bool IsFuncionario { get; set; } = false;
        public string? Cargo { get; set; }          // só usado se IsFuncionario = true
        public decimal? Salario { get; set; }       // só usado se IsFuncionario = true
    }

    public class PessoaResponseDto
    {
        public int IdPessoa { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Endereco { get; set; } = string.Empty;
        public bool IsCliente { get; set; }
        public bool IsFuncionario { get; set; }
        public string? Observacao { get; set; }
        public int QuantidadeCompras { get; set; }   // vendas já feitas para o cliente
        public decimal TotalComprado { get; set; }
        public decimal? TotalFiado { get; set; }     // só para clientes (preenchido pelo módulo Fiado)
        public string? Cargo { get; set; }           // só para funcionários
        public decimal? Salario { get; set; }        // só para funcionários
    }
}
