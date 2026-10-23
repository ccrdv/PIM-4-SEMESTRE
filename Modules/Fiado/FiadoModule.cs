namespace SistemaWhiteLabel.API.Modules.Fiado
{
    public static class FiadoModule
    {
        public const string NOME = "fiado";     // nome em tbl_Modulo

        // Chamado no Program.cs: registra tudo que o módulo precisa
        public static IServiceCollection AddFiadoModule(this IServiceCollection services)
        {
            services.AddScoped<IFiadoService, FiadoService>();
            services.AddScoped<IFiadoGateway, FiadoGateway>();
            return services;
        }
    }
}
