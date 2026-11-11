# Sistema White Label – Aplicativo mobile (PIM)

Aplicativo **Flutter** do **Sistema White Label**: versão enxuta para o dia a dia, focada em **controle de estoque**, **movimentação de itens** e **cadastro de produtos**. Conecta-se à API por **QR code**, sem digitar usuário e senha no celular.

Projeto Integrador Multidisciplinar (PIM) do 4º semestre – ADS, UNIP.

```
PIM-4-SEMESTRE
├── API-WEB    ← API e interface web (gera o QR code)
├── Banco      ← scripts SQL do Oracle
└── API-APP    ← você está aqui
```

## Como funciona

1. No sistema web, o usuário vai em **Configurações → Conectar celular** e o sistema gera um QR code.
2. No aplicativo, a tela de login abre a câmera e lê o código. O QR contém o endereço da API e um código de pareamento (vale **2 minutos** e só pode ser usado **uma vez**).
3. O aplicativo troca o código por um token na API (`POST /api/Dispositivo/parear`). A sessão dura **3 dias** e pode ser renovada até **30 dias**. O acesso do aparelho pode ser revogado no sistema web.
4. Os dados ficam em um banco **SQLite local**: as alterações são gravadas primeiro no aparelho e sincronizadas com a API. Os registros que ainda não foram enviados ficam marcados como **pendentes**.

> O celular precisa alcançar o endereço da API: a mesma rede local do computador servidor (ou uma VPN).

## Tecnologias

| Pacote | Uso |
|---|---|
| Flutter / Dart (`sdk: ^3.13.4`) | Aplicativo |
| `sqflite` + `path` | Banco SQLite local |
| `provider` | Gerenciamento de estado |
| `mobile_scanner` | Leitura do QR code pela câmera |
| `shared_preferences` | Guarda a sessão e o endereço da API |
| `http` | Comunicação com a API |

## Estrutura da pasta

```
API-APP
├── lib/
│   ├── main.dart, homepage.dart
│   ├── core/            tema, rotas e navegação
│   ├── models/          Produto, Categoria, Venda, Item_Venda, Compra, Item_Compra
│   ├── data/
│   │   ├── api/         comunicação com a API, autenticação, sessão e sincronização
│   │   ├── db/          banco SQLite local
│   │   └── repositories/ acesso aos dados locais
│   └── ui/telas/        Login (QR), Geral, Extrato e Cadastro
├── android/ ios/ ...    projetos de plataforma gerados pelo Flutter
└── pubspec.yaml
```

## Como rodar

### Requisitos
- [Flutter SDK](https://docs.flutter.dev/get-started/install) e Android Studio (ou um celular Android com depuração USB)
- A [API-WEB](../API-WEB) em execução, acessível pela rede

### Passos
```bash
flutter pub get
flutter run
```

1. Suba a API e abra o sistema web pelo **IP da rede** (não por `localhost`).
2. Gere o QR code em **Configurações → Conectar celular**.
3. No aplicativo, toque em ler QR code e aponte a câmera.

O guia técnico do pareamento está em [`API-WEB/docs/APP_MOBILE.md`](../API-WEB/docs/APP_MOBILE.md).

## Observações

- Os arquivos `PIM_*.db` são bancos locais de teste gerados no desenvolvimento; **não devem ir para o repositório** (coloque `*.db` no `.gitignore`, junto com `build/`)
- O aplicativo cobre apenas estoque e cadastro de produtos; vendas, relatórios, fornecedores e demais módulos ficam na versão web

## Pontos em desenvolvimento

- Validar o token automaticamente ao abrir o aplicativo
- Gravar sempre primeiro no banco local e enviar as alterações pendentes ao tocar em atualizar e ao sair do aplicativo
- Fila de envio de alterações feitas sem conexão
