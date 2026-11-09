namespace SistemaWhiteLabel.API.Modules.Fornecedor
{
    public static class FornecedorModule
    {
        public const string NOME = "fornecedor";     // nome em tb_modulo

        // Chamado no Program.cs: registra tudo que o módulo precisa
        public static IServiceCollection AddFornecedorModule(this IServiceCollection services)
        {
            services.AddScoped<IFornecedorService, FornecedorService>();
            services.AddScoped<ICompraService, CompraService>();
            return services;
        }
    }
}
