# Sistema White Label – Banco de dados

Scripts SQL do banco **Oracle** do **Sistema White Label**, esquema `PIM`. São scripts SQL comuns, rodados na mão (sem migrações do Entity Framework).

Projeto Integrador Multidisciplinar (PIM) do 4º semestre – ADS, UNIP.

```
PIM-4-SEMESTRE
├── API-WEB    ← API e interface web
├── Banco      ← você está aqui
└── API-Mobile    ← aplicativo Flutter
```

## Como executar

Conectado como `PIM` (SQL Developer → *Executar script*), rode **nesta ordem**:

| Script | O que faz |
|---|---|
| `01_pim_corrections.sql` | Pagamentos por venda, perfis, nomes, tipos, constraints, FKs, dados iniciais e views |
| `02_pim_ajustes_app.sql` | Endereço em texto livre e usuário inicial `admin` |
| `03_modulo_fiado.sql` | Tabela de módulos e módulo **A prazo** |
| `04_ajustes_novo_front.sql` | Estoque mínimo por produto, observação, CPF obrigatório, categoria "Geral" e formas de pagamento |
| `05_normalizacao_nomes.sql` | **Padroniza os nomes**, confere os tipos, cria `tb_dispositivo` e recria as views |
| `06_modulo_fornecedor.sql` | Módulo **Fornecedor**: registro em `tb_modulo`, CNPJ único, status e regras nas tabelas de compra |
| `07_ajuste_identity.sql` | Realinha o contador de ids de todas as tabelas. Resolve erro de id (ORA-00001) ao cadastrar produto pelo celular ou pela web |
| `08_procedures_views.sql` | **Procedures** de estoque (`sp_baixar_estoque_venda`, `sp_receber_compra`) e views de relatório. **Obrigatório:** a API chama as procedures ao vender e ao receber uma compra |

- **Banco novo:** rode 01, 02, 03, 04, 05 e, se for usar o módulo Fornecedor, o 06.
- O **07** pode ser rodado a qualquer momento depois do 05, e de novo sempre que um cadastro der erro de id.
- **Banco que já passou pelo 01–04:** rode só o 05 (e o 06).
- Os scripts 01 a 04 são o **histórico** e usam os nomes antigos (`tbl_`, maiúsculas); o 05 converte tudo para o padrão atual.
- No 06, os erros `ORA-01442`, `ORA-02260`, `ORA-02264`, `ORA-02275` e `ORA-00955` significam "já existe" e podem ser ignorados. `ORA-02296` ou `ORA-01407` indicam linhas antigas incompletas nas tabelas de compra.

## Dados de teste (opcional)

Na pasta `teste/`, fora da ordem de instalação (rode depois dos scripts 01 a 08):

| Script | O que faz |
|---|---|
| `teste/popular_dados_teste.sql` | Cria usuários `gerente` e `vendedor` (senha `teste123`), 8 clientes, 25 produtos em 5 categorias, 5 fornecedores, 4 compras, 60 vendas dos últimos 30 dias e 6 vendas a prazo. Pode rodar mais de uma vez |
| `teste/limpar_dados_teste.sql` | Apaga só o que o script acima criou |
| `teste/renumerar_produtos.sql` | Renumera os produtos de 1 a 5, trocando o id também nos itens de venda e de compra (mantém o histórico) |

## Padrão de nomes

- Tabelas com prefixo **`tb_`**, sempre no **singular**
- Todos os identificadores em **minúsculas**
- Views com prefixo **`sv_`**

Depois do script 05, escreva sempre com aspas e em minúsculas:

```sql
SELECT * FROM "PIM"."tb_pessoa";
```

## Principais tabelas

| Tabela | Função |
|---|---|
| `tb_pessoa`, `tb_endereco`, `tb_municipio`, `tb_uf` | Pessoas (clientes) e endereços |
| `tb_usuario`, `tb_perfil` | Usuários (com a chave do 2FA) e perfis Administrador, Gerente e Vendedor |
| `tb_produto`, `tb_categoria`, `tb_unidade_medida` | Produtos, com estoque e estoque mínimo |
| `tb_venda`, `tb_item_venda` | Vendas e seus itens |
| `tb_forma_pagamento`, `tb_tipo_pagamento` | Formas de pagamento |
| `tb_distribuidora` | Fornecedores (módulo Fornecedor) |
| `tb_compra`, `tb_item_compra`, `tb_status_compra` | Compras a fornecedores (Pendente, Concluído, Cancelado) |
| `tb_modulo` | Módulos do sistema e se estão ligados |
| `tb_dispositivo` | Celulares conectados por QR code |

## Views

| View | Conteúdo |
|---|---|
| `sv_cliente_dados` | Dados do cliente e o seu endereço |
| `sv_usuario_dados` | Dados do usuário e perfil, sem senha nem chave do 2FA |
| `sv_venda_relatorio` | Cada item vendido, com produto, valores, cliente e vendedor |
| `sv_vendas_relatorio_fechamento` | Fechamento por dia: quantidade de vendas, bruto, descontos, líquido e totais por forma de pagamento. É lida pela tela **Relatórios** |

## Procedures

Procedures PL/SQL com prefixo **`sp_`**, criadas no script 08 e chamadas pela API dentro da mesma transação:

| Procedure | O que faz |
|---|---|
| `sp_baixar_estoque_venda(id_venda)` | Tira do estoque os itens da venda. Se faltar estoque de algum produto, dá erro e a API desfaz a venda inteira |
| `sp_receber_compra(id_compra)` | A compra Pendente vira Concluído, a quantidade de cada item entra no estoque e o preço de compra do produto é atualizado |

## Observações

- O usuário inicial `admin` é criado pelo script 02; **troque a senha** no primeiro acesso
- A API usa o esquema `PIM` e se conecta pelo `appsettings.json` (veja o [README da API-WEB](../API-WEB/README.md))
