-- =====================================================================
-- SistemaWhiteLabel - APAGA os dados criados por popular_dados_teste.sql
-- Rode conectado como PIM. Nao mexe no admin nem no que voce cadastrou na mao.
-- Produtos, fornecedores e categorias que ja tem outros registros ligados
-- (ex.: uma venda sua) sao mantidos, e o script avisa quais.
-- =====================================================================
SET DEFINE OFF;
SET SERVEROUTPUT ON;

DECLARE
  v_gerente  NUMBER;
  v_vendedor NUMBER;

  PROCEDURE tenta(p_rotulo VARCHAR2, p_sql VARCHAR2) IS
  BEGIN
    EXECUTE IMMEDIATE p_sql;
  EXCEPTION WHEN OTHERS THEN
    DBMS_OUTPUT.PUT_LINE('mantido (em uso): ' || p_rotulo);
  END;
BEGIN
  BEGIN SELECT "id_usuario" INTO v_gerente  FROM "PIM"."tb_usuario" WHERE "username" = N'gerente';  EXCEPTION WHEN NO_DATA_FOUND THEN v_gerente := -1; END;
  BEGIN SELECT "id_usuario" INTO v_vendedor FROM "PIM"."tb_usuario" WHERE "username" = N'vendedor'; EXCEPTION WHEN NO_DATA_FOUND THEN v_vendedor := -1; END;

  -- vendas dos usuarios de teste (e o que depende delas)
  DELETE FROM "PIM"."tb_fiado" WHERE "id_venda" IN (SELECT "id_venda" FROM "PIM"."tb_venda" WHERE "id_usuario" IN (v_gerente, v_vendedor));
  DELETE FROM "PIM"."tb_forma_pagamento" WHERE "id_venda" IN (SELECT "id_venda" FROM "PIM"."tb_venda" WHERE "id_usuario" IN (v_gerente, v_vendedor));
  DELETE FROM "PIM"."tb_item_venda" WHERE "id_venda" IN (SELECT "id_venda" FROM "PIM"."tb_venda" WHERE "id_usuario" IN (v_gerente, v_vendedor));
  DELETE FROM "PIM"."tb_venda" WHERE "id_usuario" IN (v_gerente, v_vendedor);

  -- compras do gerente de teste
  DELETE FROM "PIM"."tb_item_compra" WHERE "id_compra" IN (SELECT "id_compra" FROM "PIM"."tb_compra" WHERE "id_usuario" = v_gerente);
  DELETE FROM "PIM"."tb_compra" WHERE "id_usuario" = v_gerente;

  -- celulares conectados, usuarios e funcionarios de teste
  DELETE FROM "PIM"."tb_dispositivo" WHERE "id_usuario" IN (v_gerente, v_vendedor);
  DELETE FROM "PIM"."tb_usuario" WHERE "id_usuario" IN (v_gerente, v_vendedor);
  DELETE FROM "PIM"."tb_funcionario" WHERE "id_pessoa" IN (SELECT "id_pessoa" FROM "PIM"."tb_pessoa" WHERE "observacao_pessoa" = N'DADOS DE TESTE');

  -- clientes de teste
  DELETE FROM "PIM"."tb_endereco" WHERE "id_pessoa" IN (SELECT "id_pessoa" FROM "PIM"."tb_pessoa" WHERE "observacao_pessoa" = N'DADOS DE TESTE');
  FOR c IN (SELECT "id_pessoa", "nome_pessoa" FROM "PIM"."tb_pessoa" WHERE "observacao_pessoa" = N'DADOS DE TESTE') LOOP
    tenta('cliente ' || c."nome_pessoa", 'DELETE FROM "PIM"."tb_pessoa" WHERE "id_pessoa" = ' || c."id_pessoa");
  END LOOP;

  -- produtos de teste
  FOR p IN (SELECT "id_produto", "descricao_produto" FROM "PIM"."tb_produto" WHERE "descricao_produto" IN (
      N'Refrigerante Cola 2L',
      N'Refrigerante Guaraná 2L',
      N'Água Mineral 500ml',
      N'Suco de Laranja 1L',
      N'Cerveja Lata 350ml',
      N'Energético Lata 250ml',
      N'Arroz Branco 5kg',
      N'Feijão Carioca 1kg',
      N'Macarrão Espaguete 500g',
      N'Açúcar Refinado 1kg',
      N'Café Torrado 500g',
      N'Óleo de Soja 900ml',
      N'Leite Integral 1L',
      N'Biscoito Recheado 130g',
      N'Farinha de Trigo 1kg',
      N'Sabonete 85g',
      N'Shampoo 350ml',
      N'Papel Higiênico 12 rolos',
      N'Creme Dental 90g',
      N'Detergente 500ml',
      N'Sabão em Pó 1kg',
      N'Água Sanitária 1L',
      N'Banana Prata',
      N'Tomate',
      N'Cebola')) LOOP
    tenta('produto ' || p."descricao_produto", 'DELETE FROM "PIM"."tb_produto" WHERE "id_produto" = ' || p."id_produto");
  END LOOP;

  -- fornecedores de teste
  FOR f IN (SELECT "id_distribuidora", "nome_distribuidora" FROM "PIM"."tb_distribuidora" WHERE "cnpj" IN (N'11222333000181', N'11444777000161', N'22233344000183', N'33344455000183', N'44455566000183')) LOOP
    tenta('fornecedor ' || f."nome_distribuidora", 'DELETE FROM "PIM"."tb_distribuidora" WHERE "id_distribuidora" = ' || f."id_distribuidora");
  END LOOP;

  -- categorias de teste (so as que ficaram sem produto)
  FOR c IN (SELECT "id_categoria", "descricao" FROM "PIM"."tb_categoria" WHERE "descricao" IN (N'Bebidas', N'Mercearia', N'Higiene', N'Limpeza', N'Hortifruti')) LOOP
    tenta('categoria ' || c."descricao", 'DELETE FROM "PIM"."tb_categoria" WHERE "id_categoria" = ' || c."id_categoria");
  END LOOP;

  COMMIT;
  DBMS_OUTPUT.PUT_LINE('dados de teste removidos');
END;
/
