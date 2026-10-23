-- =====================================================================
-- SistemaWhiteLabel - ajustes do banco para o novo front (parte 4)
-- Rode conectado como PIM, DEPOIS do 01, 02 e 03.
-- Se algum comando disser que a coluna/restricao ja existe (ORA-01430, ORA-01442),
-- pode ignorar e seguir para o proximo.
-- =====================================================================
SET DEFINE OFF;

-- 1. PRODUTO: estoque minimo POR ITEM (o alerta de estoque baixo usa este valor).
--    Produtos que ja existem ficam com 10, que era o valor fixo de antes.
ALTER TABLE "PIM"."tbl_Produto" ADD ("Estoque_Minimo_Produto" NUMBER(10,0) DEFAULT 10 NOT NULL);

-- 2. PESSOA: observacoes do cliente e CPF obrigatorio
ALTER TABLE "PIM"."tbl_Pessoa" ADD ("Observacao_Pessoa" NVARCHAR2(500));
ALTER TABLE "PIM"."tbl_Pessoa" MODIFY ("CPF_Pessoa" NOT NULL);

-- 3. CATEGORIA PADRAO: todo produto novo entra em "Geral" se nada for escolhido
INSERT INTO "PIM"."tbl_Categoria" ("Descricao")
SELECT N'Geral' FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM "PIM"."tbl_Categoria" WHERE UPPER("Descricao") = N'GERAL');

-- 4. FORMAS DE PAGAMENTO usadas na tela: Dinheiro, Pix e Cartao (Fiado nao tem linha aqui)
INSERT INTO "PIM"."tbl_Tipo_Pagamento" ("Descricao")
SELECT N'Dinheiro' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM "PIM"."tbl_Tipo_Pagamento" WHERE UPPER("Descricao") = N'DINHEIRO');
INSERT INTO "PIM"."tbl_Tipo_Pagamento" ("Descricao")
SELECT N'Pix' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM "PIM"."tbl_Tipo_Pagamento" WHERE UPPER("Descricao") = N'PIX');
INSERT INTO "PIM"."tbl_Tipo_Pagamento" ("Descricao")
SELECT N'Cartão' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM "PIM"."tbl_Tipo_Pagamento" WHERE UPPER("Descricao") = N'CARTÃO');

COMMIT;

-- 5. CONFERENCIA
SELECT "Id_Categoria", "Descricao" FROM "PIM"."tbl_Categoria" ORDER BY 1;
SELECT "Id_Tipo_Pagamento", "Descricao" FROM "PIM"."tbl_Tipo_Pagamento" ORDER BY 1;
SELECT "Id_Produto", "Descricao_Produto", "Estoque_Minimo_Produto" FROM "PIM"."tbl_Produto" ORDER BY 1;
