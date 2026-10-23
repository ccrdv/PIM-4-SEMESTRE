using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SistemaWhiteLabel.API.Services.Interfaces;

namespace SistemaWhiteLabel.API.Filters
{
    // Uso: [ModuloAtivo("fiado")] em cima do controller ou da action.
    // Com o módulo desligado a rota responde 404, como se ela não existisse.
    public class ModuloAtivoAttribute : TypeFilterAttribute
    {
        public ModuloAtivoAttribute(string nomeModulo) : base(typeof(ModuloAtivoFilter))
        {
            Arguments = new object[] { nomeModulo };
        }

        private class ModuloAtivoFilter : IAsyncActionFilter
        {
            private readonly string _nomeModulo;
            private readonly IModuloService _moduloService;

            public ModuloAtivoFilter(string nomeModulo, IModuloService moduloService)
            {
                _nomeModulo = nomeModulo;
                _moduloService = moduloService;
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                if (!await _moduloService.EstaAtivoAsync(_nomeModulo))
                {
                    context.Result = new NotFoundObjectResult(new { mensagem = $"O módulo '{_nomeModulo}' está desativado." });
                    return;
                }

                await next();
            }
        }
    }
}
