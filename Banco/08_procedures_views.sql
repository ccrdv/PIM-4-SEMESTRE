-- =====================================================================
-- SistemaWhiteLabel - 08: PROCEDURES de estoque e VIEWS de relatorio. Rode conectado como PIM, depois do 07.
-- Pode rodar mais de uma vez (tudo e CREATE OR REPLACE).
--
-- A API passa a chamar as duas procedures (sem elas a venda e o recebimento de compra dao erro):
--   sp_baixar_estoque_venda  - tira do estoque os itens de uma venda; sem estoque suficiente, da erro e nada muda
--   sp_receber_compra        - compra Pendente vira Concluido, soma os itens no estoque e atualiza o custo
-- E a tela Relatorios le a view sv_vendas_relatorio_fechamento.
-- =====================================================================
SET DEFINE OFF;

-- ---------------------------------------------------------------------
-- 1. PROCEDURES
-- ---------------------------------------------------------------------
-- Chamada pela API logo depois de gravar a venda e os itens, na mesma transacao:
-- se der erro aqui, a API desfaz a venda inteira
CREATE OR REPLACE PROCEDURE "PIM"."sp_baixar_estoque_venda" (p_id_venda IN NUMBER) AS
  v_itens     NUMBER;
  v_descricao VARCHAR2(600);
  v_estoque   NUMBER;
BEGIN
  SELECT COUNT(*) INTO v_itens FROM "PIM"."tb_item_venda" WHERE "id_venda" = p_id_venda;
  IF v_itens = 0 THEN
    RAISE_APPLICATION_ERROR(-20010, 'A venda ' || p_id_venda || ' não tem itens.');
  END IF;

  -- um produto pode aparecer em mais de um item: soma antes de baixar
  FOR r IN (SELECT "id_produto" AS id_produto, SUM("qdte_item_venda") AS qtde
              FROM "PIM"."tb_item_venda"
             WHERE "id_venda" = p_id_venda
             GROUP BY "id_produto"
             ORDER BY "id_produto") LOOP

    -- so baixa se ainda houver estoque (vale mesmo com duas vendas ao mesmo tempo)
    UPDATE "PIM"."tb_produto"
       SET "qdte_produto" = "qdte_produto" - r.qtde
     WHERE "id_produto" = r.id_produto
       AND "qdte_produto" >= r.qtde;

    IF SQL%ROWCOUNT = 0 THEN
      SELECT TO_CHAR("descricao_produto"), "qdte_produto" INTO v_descricao, v_estoque
        FROM "PIM"."tb_produto" WHERE "id_produto" = r.id_produto;
      RAISE_APPLICATION_ERROR(-20011, 'Estoque insuficiente! ' || v_descricao || ' | Disponível: ' || v_estoque);
    END IF;
  END LOOP;
END;
/
SHOW ERRORS;

-- A mercadoria chegou: soma no estoque, atualiza o custo do produto (custo por unidade ja descontado)
-- e a compra vira Concluido
CREATE OR REPLACE PROCEDURE "PIM"."sp_receber_compra" (p_id_compra IN NUMBER) AS
  v_status    VARCHAR2(100);
  v_concluido NUMBER;
BEGIN
  BEGIN
    SELECT TO_CHAR(s."descricao") INTO v_status
      FROM "PIM"."tb_compra" c
      JOIN "PIM"."tb_status_compra" s ON s."id_status_compra" = c."id_status_compra"
     WHERE c."id_compra" = p_id_compra
       FOR UPDATE OF c."id_status_compra";
  EXCEPTION WHEN NO_DATA_FOUND THEN
    RAISE_APPLICATION_ERROR(-20020, 'Compra não encontrada.');
  END;

  IF v_status <> 'Pendente' THEN
    RAISE_APPLICATION_ERROR(-20021, 'Só é possível receber uma compra Pendente.');
  END IF;

  BEGIN
    SELECT "id_status_compra" INTO v_concluido FROM "PIM"."tb_status_compra" WHERE "descricao" = N'Concluido';
  EXCEPTION WHEN NO_DATA_FOUND THEN
    RAISE_APPLICATION_ERROR(-20022, 'Status de compra Concluido não cadastrado em tb_status_compra (rode o 06_modulo_fornecedor.sql).');
  END;

  FOR r IN (SELECT "id_produto" AS id_produto, "qdte_item_compra" AS qtde, "valor_total_item_compra" AS valor
              FROM "PIM"."tb_item_compra"
             WHERE "id_compra" = p_id_compra
             ORDER BY "id_item_compra") LOOP
    UPDATE "PIM"."tb_produto"
       SET "qdte_produto" = "qdte_produto" + r.qtde,
           "preco_compra_produto" = ROUND(r.valor / r.qtde, 2)
     WHERE "id_produto" = r.id_produto;
  END LOOP;

  UPDATE "PIM"."tb_compra" SET "id_status_compra" = v_concluido WHERE "id_compra" = p_id_compra;
END;
/
SHOW ERRORS;

-- ---------------------------------------------------------------------
-- 2. VIEWS
-- ---------------------------------------------------------------------
-- cliente com o endereco
CREATE OR REPLACE VIEW "PIM"."sv_cliente_dados" AS
SELECT p."id_pessoa", p."nome_pessoa", p."cpf_pessoa", p."telefone_pessoa",
       e."logradouro", e."numero", e."bairro", e."cep"
FROM "PIM"."tb_pessoa" p
LEFT JOIN "PIM"."tb_endereco" e ON e."id_pessoa" = p."id_pessoa";

-- fechamento por dia (vendas canceladas ficam de fora). bruto = sem desconto, liquido = com desconto.
-- os pagamentos sao somados a parte para uma venda com varios pagamentos nao ser contada duas vezes
CREATE OR REPLACE VIEW "PIM"."sv_vendas_relatorio_fechamento" AS
SELECT d."data_fechamento", d."qtde_vendas",
       d."total_liquido" + d."total_descontos" AS "total_bruto",
       d."total_descontos", d."total_liquido",
       NVL(p."total_dinheiro", 0) AS "total_dinheiro",
       NVL(p."total_pix", 0)      AS "total_pix",
       NVL(p."total_cartao", 0)   AS "total_cartao"
FROM (
  SELECT TRUNC(v."data_hora_venda") AS "data_fechamento",
         COUNT(*) AS "qtde_vendas",
         NVL(SUM(v."total_venda"), 0) AS "total_liquido",
         NVL(SUM(v."total_desconto_venda"), 0) AS "total_descontos"
  FROM "PIM"."tb_venda" v
  WHERE v."status_venda_venda" <> 'Cancelado'
  GROUP BY TRUNC(v."data_hora_venda")
) d
LEFT JOIN (
  SELECT TRUNC(v."data_hora_venda") AS "data_fechamento",
         SUM(CASE WHEN UPPER(tp."descricao") = N'DINHEIRO' THEN fp."total_forma_pagamento" ELSE 0 END) AS "total_dinheiro",
         SUM(CASE WHEN UPPER(tp."descricao") = N'PIX'      THEN fp."total_forma_pagamento" ELSE 0 END) AS "total_pix",
         SUM(CASE WHEN UPPER(tp."descricao") = N'CARTÃO'   THEN fp."total_forma_pagamento" ELSE 0 END) AS "total_cartao"
  FROM "PIM"."tb_venda" v
  JOIN "PIM"."tb_forma_pagamento" fp ON fp."id_venda" = v."id_venda"
  JOIN "PIM"."tb_tipo_pagamento" tp  ON tp."id_tipo_pagamento" = fp."id_tipo_pagamento"
  WHERE v."status_venda_venda" <> 'Cancelado'
  GROUP BY TRUNC(v."data_hora_venda")
) p ON p."data_fechamento" = d."data_fechamento";

-- cada item vendido, com o cliente e o vendedor
CREATE OR REPLACE VIEW "PIM"."sv_venda_relatorio" AS
SELECT v."id_venda", v."data_hora_venda", pr."descricao_produto",
       iv."qdte_item_venda", iv."preco_unitario_item_venda", iv."valor_total_item_venda",
       p."nome_pessoa", p."cpf_pessoa", u."username" AS "vendedor"
FROM "PIM"."tb_venda" v
JOIN "PIM"."tb_item_venda" iv ON iv."id_venda" = v."id_venda"
JOIN "PIM"."tb_produto" pr    ON pr."id_produto" = iv."id_produto"
LEFT JOIN "PIM"."tb_usuario" u ON u."id_usuario" = v."id_usuario"
LEFT JOIN "PIM"."tb_pessoa" p ON p."id_pessoa" = v."id_pessoa"
WHERE v."status_venda_venda" = 'Concluido';

-- ---------------------------------------------------------------------
-- 3. CONFERENCIA: as 2 procedures e as 4 views devem aparecer como VALID
-- ---------------------------------------------------------------------
SELECT object_type, object_name, status FROM user_objects
 WHERE object_type IN ('PROCEDURE', 'VIEW') AND object_name NOT LIKE 'BIN$%'
 ORDER BY object_type, object_name;
