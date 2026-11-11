-- =====================================================================
-- SistemaWhiteLabel - 07: realinha os contadores de id (identity)
-- Rode conectado como PIM, depois do 05.
--
-- Sintoma: ao cadastrar um produto (pelo celular ou pela web) a API devolve erro de id,
-- ou ORA-00001 (chave repetida). Acontece quando existem linhas com ids maiores do que o
-- contador automatico da tabela conhece (linhas inseridas na mao, restauradas de backup,
-- ou de uma versao antiga do app que mandava o proprio id).
--
-- Para conferir antes (tb_produto): o maior id e o proximo valor do contador
--   SELECT MAX("id_produto") FROM "PIM"."tb_produto";
--   SELECT s.sequence_name, s.last_number
--     FROM user_sequences s JOIN user_tab_identity_cols i ON i.sequence_name = s.sequence_name
--    WHERE i.table_name = 'tb_produto';
-- Se last_number for menor ou igual ao maior id, esse e o problema.
--
-- O bloco abaixo faz o contador de TODAS as tabelas com identity comecar logo depois do
-- maior id que ja existe (START WITH LIMIT VALUE). Pode rodar de novo quando quiser.
-- =====================================================================
SET DEFINE OFF;
SET SERVEROUTPUT ON;

DECLARE
  v_max NUMBER;
BEGIN
  FOR c IN (SELECT table_name, column_name, generation_type
              FROM user_tab_identity_cols
             WHERE table_name NOT LIKE 'BIN$%'
             ORDER BY table_name) LOOP
    BEGIN
      EXECUTE IMMEDIATE 'SELECT NVL(MAX("' || c.column_name || '"), 0) FROM "PIM"."' || c.table_name || '"' INTO v_max;
      EXECUTE IMMEDIATE 'ALTER TABLE "PIM"."' || c.table_name || '" MODIFY ("' || c.column_name || '" GENERATED '
        || CASE WHEN c.generation_type = 'ALWAYS' THEN 'ALWAYS' ELSE 'BY DEFAULT ON NULL' END
        || ' AS IDENTITY (START WITH LIMIT VALUE))';
      DBMS_OUTPUT.PUT_LINE('ok   ' || c.table_name || '.' || c.column_name || ' (maior id: ' || v_max || ')');
    EXCEPTION WHEN OTHERS THEN
      DBMS_OUTPUT.PUT_LINE('ERRO ' || c.table_name || ': ' || SQLERRM);
    END;
  END LOOP;
END;
/

-- Conferencia: o contador de cada tabela tem que estar acima do maior id
SELECT i.table_name, i.column_name, s.last_number AS proximo_valor_do_contador
  FROM user_tab_identity_cols i
  JOIN user_sequences s ON s.sequence_name = i.sequence_name
 WHERE i.table_name NOT LIKE 'BIN$%'
 ORDER BY i.table_name;
