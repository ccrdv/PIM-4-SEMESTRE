-- =====================================================================
-- SistemaWhiteLabel - normalizacao dos nomes do banco (parte 5)
--
-- Rode conectado como PIM, DEPOIS dos scripts 01 a 04 (SQL Developer: "Executar script", F5).
-- Pode rodar de novo sem problema: o que ja foi renomeado e ignorado.
--
-- O QUE ESTE SCRIPT FAZ
--   1. Nomes no singular, sem plural (conferido no final: nenhuma tabela termina em "s").
--   2. Toda tabela passa a comecar com "tb_"  (tbl_Pessoa  ->  tb_pessoa).
--   3. TODOS os nomes ficam em minusculas: tabelas, colunas, chaves (PK/FK/UQ/CK),
--      indices e views (Id_Pessoa -> id_pessoa, PK_tbl_Venda -> pk_tb_venda, SV_... -> sv_...).
--   4. Confere e corrige os tipos das colunas (tamanhos de texto, flags S/N, NOT NULL).
--
-- IMPORTANTE
--   * Os nomes ficam entre aspas e minusculos ("tb_pessoa"). No SQL escreva sempre com aspas
--     e em minusculas:  SELECT * FROM "PIM"."tb_pessoa";
--   * Pare a API antes de rodar (ela segura as tabelas abertas) e use o projeto parte15,
--     que ja aponta para os novos nomes.
--   * Este script e SQL comum: nao usa migracoes do Entity Framework.
-- =====================================================================
SET DEFINE OFF;
SET SERVEROUTPUT ON SIZE UNLIMITED;

DECLARE
  -- tbl_Pessoa -> tb_pessoa, "PK_tbl_Venda" -> pk_tb_venda
  FUNCTION novo(p IN VARCHAR2) RETURN VARCHAR2 IS
  BEGIN
    RETURN LOWER(REGEXP_REPLACE(p, 'tbl_', 'tb_', 1, 0, 'i'));
  END;

  -- executa um comando e mostra o resultado; um erro nao interrompe o script
  PROCEDURE ddl(p IN VARCHAR2) IS
  BEGIN
    EXECUTE IMMEDIATE p;
    DBMS_OUTPUT.PUT_LINE('ok    ' || p);
  EXCEPTION WHEN OTHERS THEN
    DBMS_OUTPUT.PUT_LINE('ERRO  ' || SQLERRM || '  <- ' || p);
  END;

  FUNCTION existe_tabela(p IN VARCHAR2) RETURN BOOLEAN IS
    n NUMBER;
  BEGIN
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name = p;
    RETURN n > 0;
  END;
BEGIN
  -- -------------------------------------------------------------------
  -- 1. LIMPEZA do que nao serve mais
  -- -------------------------------------------------------------------
  -- views antigas: o texto delas guarda os nomes velhos, entao sao recriadas no fim
  FOR v IN (SELECT view_name FROM user_views) LOOP
    ddl('DROP VIEW "PIM"."' || v.view_name || '"');
  END LOOP;

  -- as 11 procedures SP_*_REPOSITY nao sao usadas pela API e ja estavam invalidas
  FOR o IN (SELECT object_name FROM user_objects
             WHERE object_type = 'PROCEDURE' AND object_name LIKE 'SP\_%\_REPOSITY' ESCAPE '\') LOOP
    ddl('DROP PROCEDURE "PIM"."' || o.object_name || '"');
  END LOOP;

  -- historico de migracoes do Entity Framework: o projeto nao usa mais migracoes
  IF existe_tabela('__EFMigrationsHistory') THEN
    ddl('DROP TABLE "PIM"."__EFMigrationsHistory" PURGE');
  END IF;

  -- -------------------------------------------------------------------
  -- 2. TABELAS: prefixo tb_ e minusculas
  --    Pessoa, Endereco, Funcionario, Perfil, Usuario, Categoria, Produto, Venda, Item_Venda,
  --    Forma_Pagamento, Tipo_Pagamento, Modulo, Fiado, Dispositivo, Compra, Item_Compra,
  --    Distribuidora, Status_Compra, Unidade_Medida, Municipio, Unidade_Federacao
  -- -------------------------------------------------------------------
  FOR t IN (SELECT table_name FROM user_tables WHERE table_name <> novo(table_name)) LOOP
    ddl('ALTER TABLE "PIM"."' || t.table_name || '" RENAME TO "' || novo(t.table_name) || '"');
  END LOOP;

  -- -------------------------------------------------------------------
  -- 3. COLUNAS em minusculas
  -- -------------------------------------------------------------------
  FOR c IN (SELECT table_name, column_name FROM user_tab_columns
             WHERE table_name IN (SELECT table_name FROM user_tables)
               AND hidden_column = 'NO'
               AND column_name <> LOWER(column_name)
             ORDER BY table_name, column_id) LOOP
    ddl('ALTER TABLE "PIM"."' || c.table_name || '" RENAME COLUMN "' || c.column_name || '" TO "' || LOWER(c.column_name) || '"');
  END LOOP;

  -- -------------------------------------------------------------------
  -- 4. CHAVES (PK, FK, UNIQUE, CHECK) e INDICES em minusculas, com tb_ no lugar de tbl_
  --    (as restricoes NOT NULL sem nome, SYS_C..., sao do proprio Oracle e ficam como estao)
  -- -------------------------------------------------------------------
  FOR k IN (SELECT table_name, constraint_name FROM user_constraints
             WHERE constraint_name NOT LIKE 'SYS\_%' ESCAPE '\'
               AND constraint_name <> novo(constraint_name)
             ORDER BY table_name, constraint_name) LOOP
    ddl('ALTER TABLE "PIM"."' || k.table_name || '" RENAME CONSTRAINT "' || k.constraint_name || '" TO "' || novo(k.constraint_name) || '"');
  END LOOP;

  -- indices (alguns ja foram renomeados junto com a chave; o laco so pega o que sobrou)
  FOR i IN (SELECT index_name FROM user_indexes
             WHERE index_name NOT LIKE 'SYS\_%' ESCAPE '\'
               AND index_name <> novo(index_name)
             ORDER BY index_name) LOOP
    ddl('ALTER INDEX "PIM"."' || i.index_name || '" RENAME TO "' || novo(i.index_name) || '"');
  END LOOP;

  -- -------------------------------------------------------------------
  -- 5. TIPOS DAS COLUNAS
  --    Ja corrigidos nos scripts anteriores: valores em NUMBER(12,2) (nada de BINARY_FLOAT),
  --    ids em NUMBER(10,0), datas em TIMESTAMP(7). Aqui entram os que sobraram.
  -- -------------------------------------------------------------------
  -- Pessoa: CPF so tem os 11 digitos (a API tira pontos e traco); telefone cabe em 15
  ddl('ALTER TABLE "PIM"."tb_pessoa" MODIFY ("cpf_pessoa" NVARCHAR2(11), "telefone_pessoa" NVARCHAR2(15))');

  -- Login: a chave do 2FA e um texto base32 curto (era NVARCHAR2(2000))
  ddl('ALTER TABLE "PIM"."tb_usuario" MODIFY ("chave_2fa" NVARCHAR2(64))');

  -- Unidade de medida: textos curtos e obrigatorios; flag S/N igual ao do modulo
  ddl('ALTER TABLE "PIM"."tb_unidade_medida" MODIFY ("tipo" NVARCHAR2(10) NOT NULL, "descricao_unidade_medida" NVARCHAR2(100) NOT NULL)');
  ddl('ALTER TABLE "PIM"."tb_unidade_medida" MODIFY ("ativo_unidade_medida" NVARCHAR2(1) DEFAULT N''S'' NOT NULL)');
  ddl('ALTER TABLE "PIM"."tb_unidade_medida" ADD CONSTRAINT "ck_tb_unidade_medida_ativo" CHECK ("ativo_unidade_medida" IN (N''S'', N''N''))');

  -- Status da compra: descricao curta e obrigatoria (era NVARCHAR2(2000))
  ddl('ALTER TABLE "PIM"."tb_status_compra" MODIFY ("descricao" NVARCHAR2(50) NOT NULL)');

  -- Modulo: a flag foi criada como NCHAR(1) em alguns bancos; o padrao e NVARCHAR2(1)
  ddl('ALTER TABLE "PIM"."tb_modulo" MODIFY ("ativo_modulo" NVARCHAR2(1))');

  -- Distribuidora (modulo de compras, ainda sem uso): tamanhos reais
  ddl('ALTER TABLE "PIM"."tb_distribuidora" MODIFY ("cnpj" NVARCHAR2(18), "telefone_distribuidora" NVARCHAR2(15))');

  -- -------------------------------------------------------------------
  -- 6. CELULARES AUTORIZADOS: cria a tabela se o banco ainda nao tinha
  -- -------------------------------------------------------------------
  IF NOT existe_tabela('tb_dispositivo') THEN
    ddl(q'[CREATE TABLE "PIM"."tb_dispositivo" (
             "id_dispositivo"   NUMBER(10,0) GENERATED BY DEFAULT ON NULL AS IDENTITY,
             "id_usuario"       NUMBER(10,0)  NOT NULL,
             "nome_dispositivo" NVARCHAR2(100) NOT NULL,
             "criado_em"        TIMESTAMP(7)  NOT NULL,
             "expira_em"        TIMESTAMP(7)  NOT NULL,
             "limite_em"        TIMESTAMP(7)  NOT NULL,
             "ultimo_uso"       TIMESTAMP(7),
             "revogado_em"      TIMESTAMP(7),
             CONSTRAINT "pk_tb_dispositivo" PRIMARY KEY ("id_dispositivo"),
             CONSTRAINT "fk_tb_dispositivo_tb_usuario" FOREIGN KEY ("id_usuario")
               REFERENCES "PIM"."tb_usuario" ("id_usuario") ON DELETE CASCADE
           )]');
    ddl('CREATE INDEX "ix_tb_dispositivo_id_usuario" ON "PIM"."tb_dispositivo" ("id_usuario")');
  END IF;
END;
/

-- ---------------------------------------------------------------------
-- 7. VIEWS recriadas com os novos nomes (nomes de colunas em minusculas tambem)
-- ---------------------------------------------------------------------
CREATE OR REPLACE VIEW "PIM"."sv_cliente_dados" AS
SELECT p."id_pessoa", p."nome_pessoa", p."cpf_pessoa", p."telefone_pessoa",
       e."logradouro", e."numero"
FROM "PIM"."tb_pessoa" p
LEFT JOIN "PIM"."tb_endereco" e ON e."id_pessoa" = p."id_pessoa";

CREATE OR REPLACE VIEW "PIM"."sv_usuario_dados" AS
SELECT u."id_usuario", u."username", u."status", pf."nome_perfil",
       p."nome_pessoa", p."cpf_pessoa"
FROM "PIM"."tb_usuario" u
JOIN "PIM"."tb_pessoa" p  ON p."id_pessoa" = u."id_pessoa"
JOIN "PIM"."tb_perfil" pf ON pf."id_perfil" = u."id_perfil";

-- os pagamentos sao somados a parte para uma venda com varios pagamentos nao ser contada duas vezes
CREATE OR REPLACE VIEW "PIM"."sv_vendas_relatorio_fechamento" AS
SELECT d."data_fechamento", d."qtde_vendas", d."total_dia", d."total_descontos",
       NVL(p."total_dinheiro", 0) AS "total_dinheiro",
       NVL(p."total_cartao", 0)   AS "total_cartao"
FROM (
  SELECT TRUNC(v."data_hora_venda") AS "data_fechamento",
         COUNT(*) AS "qtde_vendas",
         SUM(CASE WHEN v."status_venda_venda" <> 'Cancelado' THEN v."total_venda" ELSE 0 END) AS "total_dia",
         SUM(v."total_desconto_venda") AS "total_descontos"
  FROM "PIM"."tb_venda" v
  GROUP BY TRUNC(v."data_hora_venda")
) d
LEFT JOIN (
  SELECT TRUNC(v."data_hora_venda") AS "data_fechamento",
         SUM(CASE WHEN tp."descricao" = N'Dinheiro' THEN fp."total_forma_pagamento" ELSE 0 END) AS "total_dinheiro",
         SUM(CASE WHEN tp."descricao" = N'Cartão'   THEN fp."total_forma_pagamento" ELSE 0 END) AS "total_cartao"
  FROM "PIM"."tb_venda" v
  JOIN "PIM"."tb_forma_pagamento" fp ON fp."id_venda" = v."id_venda"
  JOIN "PIM"."tb_tipo_pagamento" tp  ON tp."id_tipo_pagamento" = fp."id_tipo_pagamento"
  WHERE v."status_venda_venda" <> 'Cancelado'
  GROUP BY TRUNC(v."data_hora_venda")
) p ON p."data_fechamento" = d."data_fechamento";

CREATE OR REPLACE VIEW "PIM"."sv_venda_relatorio" AS
SELECT v."id_venda", v."data_hora_venda", pr."descricao_produto",
       iv."qdte_item_venda", iv."preco_unitario_item_venda", iv."valor_total_item_venda",
       p."nome_pessoa", p."cpf_pessoa"
FROM "PIM"."tb_venda" v
JOIN "PIM"."tb_item_venda" iv ON iv."id_venda" = v."id_venda"
JOIN "PIM"."tb_produto" pr    ON pr."id_produto" = iv."id_produto"
LEFT JOIN "PIM"."tb_pessoa" p ON p."id_pessoa" = v."id_pessoa"
WHERE v."status_venda_venda" = 'Concluido';

-- ---------------------------------------------------------------------
-- 8. CONFERENCIA (cada consulta abaixo deve voltar SEM LINHAS, exceto a ultima)
-- ---------------------------------------------------------------------
-- tabelas fora do padrao (nao minusculas ou sem o prefixo tb_)
SELECT table_name AS tabela_fora_do_padrao FROM user_tables
 WHERE table_name <> LOWER(table_name) OR table_name NOT LIKE 'tb\_%' ESCAPE '\';

-- tabelas com nome no plural
SELECT table_name AS tabela_no_plural FROM user_tables
 WHERE REGEXP_LIKE(table_name, '(s|es)$');

-- colunas com letra maiuscula
SELECT table_name, column_name AS coluna_fora_do_padrao FROM user_tab_columns
 WHERE table_name IN (SELECT table_name FROM user_tables UNION SELECT view_name FROM user_views)
   AND column_name <> LOWER(column_name);

-- chaves e indices com letra maiuscula (os SYS_C... do Oracle nao contam)
SELECT constraint_name AS chave_fora_do_padrao FROM user_constraints
 WHERE constraint_name <> LOWER(constraint_name) AND constraint_name NOT LIKE 'SYS\_%' ESCAPE '\'
UNION ALL
SELECT index_name FROM user_indexes
 WHERE index_name <> LOWER(index_name) AND index_name NOT LIKE 'SYS\_%' ESCAPE '\';

-- objetos invalidos (views devem estar VALID)
SELECT object_name, object_type, status AS objeto_invalido FROM user_objects WHERE status <> 'VALID';

-- tipos que ainda merecem olhar: BINARY_FLOAT/NCHAR ou texto maior que 500
SELECT table_name, column_name, data_type, char_length AS tipo_suspeito FROM user_tab_columns
 WHERE table_name IN (SELECT table_name FROM user_tables)
   AND (data_type IN ('BINARY_FLOAT', 'BINARY_DOUBLE', 'FLOAT', 'NCHAR', 'CHAR') OR char_length > 500);

-- lista final das tabelas e colunas
SELECT table_name, column_id, column_name, data_type, data_length, data_precision, data_scale, nullable
  FROM user_tab_columns
 WHERE table_name IN (SELECT table_name FROM user_tables)
 ORDER BY table_name, column_id;
