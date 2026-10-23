using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using SistemaWhiteLabel.API.Helpers;
using SistemaWhiteLabel.API.Data;
using SistemaWhiteLabel.API.Repositories.Interfaces;
using SistemaWhiteLabel.API.Repositories;
using SistemaWhiteLabel.API.Services.Interfaces;
using SistemaWhiteLabel.API.Services;
using SistemaWhiteLabel.API.Modules.Fiado;

var builder = WebApplication.CreateBuilder(args);

// ====================== CONFIGURAÇÕES ======================

// Oracle Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleConnection")));

// Cache em memória (usado só para o limite de tentativas do login)
builder.Services.AddMemoryCache();

// ====================== CORS ======================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// ========== CONFIGURAÇÃO DO JWT ==========
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"] ?? "MinhaChaveSuperSecretaComPeloMenos32Caracteres123";
var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.Configure<JwtSettings>(jwtSettings);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// ========== AUTORIZAÇÃO (LOGIN EM 2 ETAPAS) ==========
builder.Services.AddAuthorization(options =>
{
    // Sessão completa (usuário + senha + código do 2FA): vale para o sistema todo
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireClaim("etapa", "sessao")
        .Build();

    // Token temporário: só vale nos passos do 2FA (/Auth/2fa/...)
    options.AddPolicy("Etapa2FA", policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim("etapa", "2fa"));

    // Tudo exige sessão, exceto o que tiver [AllowAnonymous] (login).
    // Para voltar ao comportamento antigo (só os [Authorize]), apague a linha abaixo.
    options.FallbackPolicy = options.DefaultPolicy;
});

// ====================== INJEÇÃO DE DEPENDÊNCIAS ======================
builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<IVendaRepository, VendaRepository>();

builder.Services.AddScoped<IProdutoService, ProdutoService>();
builder.Services.AddScoped<IPessoaService, PessoaService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IVendaService, VendaService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();
builder.Services.AddScoped<IModuloService, ModuloService>();
builder.Services.AddScoped<ComprovanteService>();

// ====================== MÓDULOS ======================
// Cada módulo se registra aqui; ligar/desligar é feito pela API (PUT /api/Modulos/{nome})
builder.Services.AddFiadoModule();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ====================== MIDDLEWARE ======================

app.UseHttpsRedirection();

// As páginas (html, js, css) precisam vir ANTES da autenticação, porque o navegador
// não envia o Bearer token ao abrir uma página. Quem protege as páginas é o protegerRota() do script.js
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Evita o navegador usar uma cópia antiga das páginas e do script.js
        ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store";
    }
});

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// A API continua protegida: tudo em /api exige o token de sessão
app.MapControllers();

app.Run();