namespace SistemaWhiteLabel.API.Modules.Fornecedor
{
    public class FornecedorDto
    {
        public int IdFornecedor { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Cnpj { get; set; } = string.Empty;
        public string? Ramo { get; set; }
        public string Status { get; set; } = "Ativo";
        public string? Telefone { get; set; }
        public string? Email { get; set; }
        public int QuantidadeCompras { get; set; }        // sem contar as canceladas
        public decimal TotalComprado { get; set; }
    }

    public class FornecedorSalvarDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Cnpj { get; set; } = string.Empty;
        public string? Ramo { get; set; }
        public string? Status { get; set; }               // Ativo (padrão) ou Inativo
        public string? Telefone { get; set; }
        public string? Email { get; set; }
    }

    public class NovaCompraDto
    {
        public int IdFornecedor { get; set; }
        public List<ItemNovaCompraDto> Itens { get; set; } = new();

        // true = a mercadoria já chegou (Concluido, soma no estoque); false = pedido Pendente
        public bool Receber { get; set; }
    }

    public class ItemNovaCompraDto
    {
        public int ProdutoId { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }        // custo de cada unidade
        public decimal? Desconto { get; set; }            // valor (R$) do item
    }

    public class CompraResponseDto
    {
        public int IdCompra { get; set; }
        public DateTime DataHora { get; set; }
        public int IdFornecedor { get; set; }
        public string NomeFornecedor { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalBruto { get; set; }           // antes do desconto
        public decimal TotalDesconto { get; set; }
        public decimal TotalCompra { get; set; }          // líquido
        public List<ItemCompraResponseDto> Itens { get; set; } = new();
    }

    public class ItemCompraResponseDto
    {
        public int ProdutoId { get; set; }
        public string NomeProduto { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal ValorDesconto { get; set; }
        public decimal ValorTotal { get; set; }
    }

    public class ResumoComprasDto
    {
        public int FornecedoresAtivos { get; set; }
        public int ComprasPendentes { get; set; }
        public decimal TotalPendente { get; set; }
        public decimal GastoNoMes { get; set; }           // compras Concluido deste mês
    }
}
