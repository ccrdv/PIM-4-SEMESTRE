-- =====================================================================
-- SistemaWhiteLabel - modulo FORNECEDOR (parte 6)
-- Rode conectado como PIM, DEPOIS do 05_normalizacao_nomes.sql (nomes tb_ em minusculas).
--
-- O modulo usa as tabelas que o banco ja tinha, sem criar nenhuma nova:
--   tb_distribuidora   = fornecedores
--   tb_compra          = compra feita a um fornecedor
--   tb_item_compra     = produtos da compra
--   tb_status_compra   = Pendente / Concluido / Cancelado
-- Aqui so entram o registro do modulo e as regras que faltavam nessas tabelas.
-- Se algum comando disser ORA-01442, ORA-02260, ORA-02264, ORA-02275 ou ORA-00955
-- (ja existe / ja foi feito), pode ignorar e seguir para o proximo.
-- As tabelas de compra precisam estar vazias ou sem valores nulos nas colunas que viram NOT NULL
-- (ORA-02296 / ORA-01407 significa que ha linhas antigas incompletas).
-- =====================================================================
SET DEFINE OFF;

-- 1. Registro do modulo (liga/desliga em Configuracoes ou PUT /api/Modulos/fornecedor)
INSERT INTO "PIM"."tb_modulo" ("nome_modulo", "descricao_modulo", "ativo_modulo")
SELECT N'fornecedor', N'Fornecedores e compras de mercadoria (a entrada no estoque)', N'S' FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM "PIM"."tb_modulo" WHERE "nome_modulo" = N'fornecedor');

-- 2. FORNECEDOR (tb_distribuidora): CNPJ so com os 14 digitos e sem repetir
ALTER TABLE "PIM"."tb_distribuidora" MODIFY (
  "nome_distribuidora" NVARCHAR2(100) NOT NULL,
  "cnpj"               NVARCHAR2(14)  NOT NULL,
  "status"             NVARCHAR2(15)  DEFAULT N'Ativo' NOT NULL);

ALTER TABLE "PIM"."tb_distribuidora" ADD CONSTRAINT "uq_tb_distribuidora_cnpj" UNIQUE ("cnpj");
ALTER TABLE "PIM"."tb_distribuidora" ADD CONSTRAINT "ck_tb_distribuidora_status"
  CHECK ("status" IN (N'Ativo', N'Inativo'));

-- 3. COMPRA (tb_compra): sempre com fornecedor, usuario e status; valores nunca nulos
ALTER TABLE "PIM"."tb_compra" MODIFY (
  "data_hora_compra"      TIMESTAMP(7)  DEFAULT SYSTIMESTAMP NOT NULL,
  "total_compra"          NUMBER(12,2)  DEFAULT 0 NOT NULL,
  "total_desconto_compra" NUMBER(12,2)  DEFAULT 0 NOT NULL,
  "id_usuario"            NUMBER(10,0)  NOT NULL,
  "id_distribuidora"      NUMBER(10,0)  NOT NULL,
  "id_status_compra"      NUMBER(10,0)  NOT NULL);

-- 4. ITEM DA COMPRA (tb_item_compra): quantidade positiva, valores nunca nulos
ALTER TABLE "PIM"."tb_item_compra" MODIFY (
  "qdte_item_compra"           NUMBER(10,0) NOT NULL,
  "preco_unitario_item_compra" NUMBER(12,2) NOT NULL,
  "valor_desconto_item_compra" NUMBER(12,2) DEFAULT 0 NOT NULL,
  "valor_total_item_compra"    NUMBER(12,2) NOT NULL,
  "id_compra"                  NUMBER(10,0) NOT NULL,
  "id_produto"                 NUMBER(10,0) NOT NULL);

ALTER TABLE "PIM"."tb_item_compra" ADD CONSTRAINT "ck_tb_item_compra_qdte" CHECK ("qdte_item_compra" > 0);

-- 5. Status da compra (ja criados no script 01; so garante que existem)
INSERT INTO "PIM"."tb_status_compra" ("descricao")
SELECT N'Pendente' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM "PIM"."tb_status_compra" WHERE "descricao" = N'Pendente');
INSERT INTO "PIM"."tb_status_compra" ("descricao")
SELECT N'Concluido' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM "PIM"."tb_status_compra" WHERE "descricao" = N'Concluido');
INSERT INTO "PIM"."tb_status_compra" ("descricao")
SELECT N'Cancelado' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM "PIM"."tb_status_compra" WHERE "descricao" = N'Cancelado');

COMMIT;

-- 6. CONFERENCIA
SELECT * FROM "PIM"."tb_modulo";
SELECT * FROM "PIM"."tb_status_compra" ORDER BY 1;
SELECT COUNT(*) AS qtd_fornecedores FROM "PIM"."tb_distribuidora";
SELECT COUNT(*) AS qtd_compras      FROM "PIM"."tb_compra";
