# Scripts do banco (Oracle, schema PIM)

Os scripts são SQL comum, rodados na mão (SQL Developer > Executar script), **nesta ordem**, conectado como `PIM`.
O projeto não usa mais migrações do Entity Framework.

| Script | O que faz |
|---|---|
| `01_pim_corrections.sql` | pagamentos por venda, perfis, nomes, tipos, constraints, FKs, dados iniciais, views |
| `02_pim_ajustes_app.sql` | endereço em texto livre e usuário `admin` inicial |
| `03_modulo_fiado.sql` | `tbl_Modulo` e `tbl_Fiado` (módulo "A prazo") |
| `04_ajustes_novo_front.sql` | estoque mínimo por produto, observação, CPF obrigatório, categoria "Geral", formas de pagamento |
| `05_normalizacao_nomes.sql` | **tabelas `tb_`, tudo em minúsculas, sem plural, tipos conferidos**, celulares autorizados (`tb_dispositivo`) e views recriadas |

Os scripts 01 a 04 são o histórico e usam os nomes antigos (`tbl_`, maiúsculas): quem já rodou 01–04 só precisa rodar o 05.
Banco novo: rode 01, 02, 03, 04 e depois 05.

Depois do 05, no SQL escreva sempre com aspas e minúsculas: `SELECT * FROM "PIM"."tb_pessoa";`
