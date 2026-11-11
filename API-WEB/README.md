# Sistema White Label – API e Web

API REST em **ASP.NET Core 9** e interface web do **Sistema White Label**, um sistema de gestão comercial para comércios de compra e revenda (mercados, distribuidores). A mesma aplicação entrega as páginas web e atende o [aplicativo mobile](../API-Mobile).

Projeto Integrador Multidisciplinar (PIM) do 4º semestre – ADS, UNIP.

## Estrutura do repositório

```
PIM-4-SEMESTRE
├── API-WEB    ← você está aqui (API + interface web)
├── Banco      ← scripts SQL do Oracle
└── API-Mobile    ← aplicativo Flutter
```

## O que o sistema faz

- Login em duas etapas: senha + código **TOTP** (aplicativo autenticador)
- Perfis de acesso: **Administrador**, **Gerente** e **Vendedor**
- Vendas (PDV) com dinheiro, Pix e cartão, desconto e **recibo em PDF**
- Produtos, categorias, estoque com **estoque mínimo** e alerta de estoque baixo
- Clientes, histórico de vendas, dashboard e **relatório de fechamento por período** (valores brutos e líquidos)
- **Módulos que podem ser ligados e desligados** em Configurações:
  - **A prazo** – vendas a prazo e valores a receber
  - **Fornecedor** – fornecedores e compras, com entrada automática no estoque
- Conexão do celular por **QR code** (sessão de 3 dias, renovável até 30)

## Tecnologias

| Camada | Tecnologia |
|---|---|
| API | C# / ASP.NET Core 9, Entity Framework Core 8 (Oracle), JWT, Swagger |
| Banco | Oracle (esquema `PIM`) – veja a pasta [Banco](../Banco) |
| Web | HTML5, CSS e JavaScript (sem framework), servidos pela própria API em `wwwroot` |
| Outros | QuestPDF (recibos), GoogleAuthenticator (TOTP) |

## Estrutura da pasta

```
API-WEB
├── Controllers/   rotas da API (Auth, Venda, Produto, Cliente, Estoque, Dispositivo...)
├── Services/      regras de negócio (Auth, Venda, Produto, Token, TwoFactor, Módulos...)
├── Modules/       módulos opcionais: Fiado (A prazo) e Fornecedor
├── Data/          ApplicationDbContext (mapeamento EF Core)
├── Models/        entidades do banco
├── DTOs/          objetos de transferência (entrada e saída da API)
├── Filters/       atributo [ModuloAtivo] (404 quando o módulo está desligado)
├── Helpers/ e Repositories/
├── wwwroot/       páginas HTML, style.css e script.js
├── docs/          guia do aplicativo mobile
├── Program.cs     configuração (JWT, políticas, módulos, arquivos estáticos)
└── appsettings.json
```

A API segue o padrão **Controller → Service → DbContext**: o controller recebe a requisição e confere o perfil; o service concentra a regra de negócio; o DbContext acessa o Oracle. A baixa de estoque da venda e o recebimento de compra são feitos por **procedures do banco** (`Banco/08_procedures_views.sql`), chamadas pelo `Helpers/ProcedureHelper.cs` dentro da transação da API.

## Como rodar

### Requisitos
- [.NET SDK 9](https://dotnet.microsoft.com/download)
- Oracle Database com o serviço `XEPDB1` e o usuário `PIM`, com os scripts da pasta [Banco](../Banco) já executados

### 1. Banco de dados
Rode os scripts em ordem, conforme o [README do Banco](../Banco/README.md).

### 2. Configuração
Copie o `appsettings.example.json` para `appsettings.json` (esse arquivo não vai para o GitHub) e preencha:

| Chave | O que é |
|---|---|
| `ConnectionStrings:OracleConnection` | Conexão com o Oracle. Formato do `Data Source`: `servidor:1521/XEPDB1` (use `localhost` se o Oracle está na mesma máquina) |
| `JwtSettings:Secret` | Chave que assina os tokens (mínimo 32 caracteres). **Troque em cada instalação** |
| `JwtSettings:AccessTokenExpirationMinutes` | Duração da sessão do navegador (60) |
| `Dispositivos:DiasDeSessao` / `LimiteDias` | Sessão do celular (3 dias) e limite com renovações (30) |

> O `appsettings.json` está no `.gitignore`: a senha do banco e a chave JWT ficam só na máquina de cada um.

### 3. Executar
```bash
dotnet run --launch-profile https
```
A aplicação sobe em `http://0.0.0.0:5012` e `https://0.0.0.0:7125` e abre a página de login.

- **Web:** `http://IP-DO-COMPUTADOR:5012`
- **Swagger:** `http://IP-DO-COMPUTADOR:5012/swagger`

Para o celular se conectar, abra o sistema pelo **IP da rede** (não por `localhost`), deixe o celular na mesma rede e libere a porta `5012` no firewall do Windows. Em desenvolvimento o redirecionamento para HTTPS fica desligado.

### 4. Primeiro acesso
O script `02_pim_ajustes_app.sql` cria o usuário inicial `admin`. **Troque a senha** no primeiro acesso. No primeiro login o sistema mostra um QR code para configurar o aplicativo autenticador; depois basta informar o código de 6 dígitos a cada login.

## Perfis

| Tela | Administrador | Gerente | Vendedor |
|---|:-:|:-:|:-:|
| Dashboard, Nova venda, Histórico, Clientes | ✔ | ✔ | ✔ |
| Produtos, Estoque, Cadastros, Relatórios | ✔ | ✔ | — |
| Fornecedores e Compras (módulo) | ✔ | ✔ | — |
| Ligar/desligar módulos | ✔ | — | — |

Novos usuários são criados pelo Administrador em `POST /api/Funcionario`.

## Principais rotas

| Recurso | Rotas |
|---|---|
| `api/Auth` | `POST login`, `2fa/configurar`, `2fa/ativar`, `2fa/verificar`, `logout`, `GET check-token` |
| `api/Venda`, `api/Comprovante` | registrar e consultar vendas; recibo em PDF |
| `api/Produto`, `api/Categoria`, `api/Estoque`, `api/Cliente` | cadastros e estoque |
| `api/Dashboard` | indicadores por período; `GET fechamento?de=&ate=` (relatório por dia, lido da view do banco) |
| `api/Modulos` | `GET` e `PUT {nome}` (liga/desliga) |
| `api/Dispositivo` | `POST pareamento` (gera o QR), `POST parear` (celular), lista, renovar e revogar |
| `api/Fiado` *(módulo)* | resumo, listar, pagar, histórico do cliente |
| `api/Fornecedor`, `api/Compra` *(módulo)* | CRUD de fornecedores; compras com `receber` e `cancelar` |

Os arquivos `SistemaWhiteLabel.API.http` e `SistemaWhiteLabel.Auth.http` trazem exemplos de chamadas. O guia de conexão do celular está em [`docs/APP_MOBILE.md`](docs/APP_MOBILE.md).

## Segurança

- Todas as rotas exigem token, exceto login e pareamento do celular
- O token do celular só vale enquanto o aparelho estiver autorizado em `tb_dispositivo` (dá para revogar)
- Cinco tentativas erradas de login, código ou pareamento bloqueiam por 5 minutos

## Limitações conhecidas

- Testado em ambiente de desenvolvimento, em rede local; ainda sem piloto em um comércio real
- Sem testes automatizados e sem pipeline de CI/CD (previstos como evolução)
- As senhas ainda são comparadas em texto puro: guardar com hash é um ponto de melhoria
