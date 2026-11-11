-- =====================================================================
-- SistemaWhiteLabel - DADOS DE TESTE (nao faz parte da instalacao)
-- Rode conectado como PIM, DEPOIS dos scripts 01 a 06 (e do 07).
--
-- Cria: 2 usuarios, 8 clientes, 5 categorias, 25 produtos, 5 fornecedores, 4 compras,
--       ~60 vendas dos ultimos 30 dias (dinheiro, Pix e cartao) e 6 vendas a prazo (com pagamentos parciais).
--
-- Usuarios novos (senha em texto puro, igual ao resto do sistema; no 1o login cada um cadastra o autenticador):
--     gerente  / teste123   (perfil Gerente)
--     vendedor / teste123   (perfil Vendedor)
--   O admin do script 02 continua existindo.
--
-- Pode rodar mais de uma vez: cada bloco confere o que ja existe e so cria o que falta
-- (as vendas e as compras so sao geradas se os usuarios de teste ainda nao tiverem nenhuma).
-- O estoque dos produtos e o valor que esta no cadastro: as vendas e compras de teste
-- sao so historico e nao mexem nele. Alguns produtos ficam abaixo do estoque minimo
-- e um fica zerado, para testar o alerta de estoque baixo.
-- Para apagar tudo isto depois: Banco/teste/limpar_dados_teste.sql
-- Dados ficticios: CPFs e CNPJs gerados so para teste.
-- =====================================================================
SET DEFINE OFF;
SET SERVEROUTPUT ON;

-- 1. CATEGORIAS
INSERT INTO "PIM"."tb_categoria" ("descricao")
SELECT N'Bebidas' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM "PIM"."tb_categoria" WHERE UPPER("descricao") = UPPER(N'Bebidas'));
INSERT INTO "PIM"."tb_categoria" ("descricao")
SELECT N'Mercearia' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM "PIM"."tb_categoria" WHERE UPPER("descricao") = UPPER(N'Mercearia'));
INSERT INTO "PIM"."tb_categoria" ("descricao")
SELECT N'Higiene' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM "PIM"."tb_categoria" WHERE UPPER("descricao") = UPPER(N'Higiene'));
INSERT INTO "PIM"."tb_categoria" ("descricao")
SELECT N'Limpeza' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM "PIM"."tb_categoria" WHERE UPPER("descricao") = UPPER(N'Limpeza'));
INSERT INTO "PIM"."tb_categoria" ("descricao")
SELECT N'Hortifruti' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM "PIM"."tb_categoria" WHERE UPPER("descricao") = UPPER(N'Hortifruti'));
COMMIT;

-- 2. USUARIOS, CLIENTES, FORNECEDORES E PRODUTOS
DECLARE
  PROCEDURE novo_usuario(p_nome NVARCHAR2, p_cpf NVARCHAR2, p_cargo NVARCHAR2, p_salario NUMBER,
                         p_user NVARCHAR2, p_perfil NVARCHAR2) IS
    v_n NUMBER; v_pessoa NUMBER; v_perfil NUMBER;
  BEGIN
    SELECT COUNT(*) INTO v_n FROM "PIM"."tb_usuario" WHERE "username" = p_user;
    IF v_n = 0 THEN
      SELECT "id_perfil" INTO v_perfil FROM "PIM"."tb_perfil" WHERE "nome_perfil" = p_perfil;
      INSERT INTO "PIM"."tb_pessoa" ("nome_pessoa", "cpf_pessoa", "observacao_pessoa")
        VALUES (p_nome, p_cpf, N'DADOS DE TESTE') RETURNING "id_pessoa" INTO v_pessoa;
      INSERT INTO "PIM"."tb_funcionario" ("id_pessoa", "cargo", "salario") VALUES (v_pessoa, p_cargo, p_salario);
      INSERT INTO "PIM"."tb_usuario" ("username", "senha", "status", "id_pessoa", "id_perfil")
        VALUES (p_user, N'teste123', N'Ativo', v_pessoa, v_perfil);
    END IF;
  END;

  PROCEDURE novo_cliente(p_nome NVARCHAR2, p_cpf NVARCHAR2, p_tel NVARCHAR2,
                         p_rua NVARCHAR2, p_num NUMBER, p_bairro NVARCHAR2, p_cep NVARCHAR2) IS
    v_n NUMBER; v_pessoa NUMBER;
  BEGIN
    SELECT COUNT(*) INTO v_n FROM "PIM"."tb_pessoa" WHERE "cpf_pessoa" = p_cpf;
    IF v_n = 0 THEN
      INSERT INTO "PIM"."tb_pessoa" ("nome_pessoa", "cpf_pessoa", "telefone_pessoa", "observacao_pessoa")
        VALUES (p_nome, p_cpf, p_tel, N'DADOS DE TESTE') RETURNING "id_pessoa" INTO v_pessoa;
      INSERT INTO "PIM"."tb_endereco" ("logradouro", "numero", "bairro", "cep", "id_pessoa")
        VALUES (p_rua, p_num, p_bairro, p_cep, v_pessoa);
    END IF;
  END;

  PROCEDURE novo_fornecedor(p_nome NVARCHAR2, p_cnpj NVARCHAR2, p_ramo NVARCHAR2, p_tel NVARCHAR2,
                            p_email NVARCHAR2, p_status NVARCHAR2) IS
    v_n NUMBER;
  BEGIN
    SELECT COUNT(*) INTO v_n FROM "PIM"."tb_distribuidora" WHERE "cnpj" = p_cnpj;
    IF v_n = 0 THEN
      INSERT INTO "PIM"."tb_distribuidora" ("nome_distribuidora", "cnpj", "ramo", "status", "telefone_distribuidora", "email_distribuidora")
        VALUES (p_nome, p_cnpj, p_ramo, p_status, p_tel, p_email);
    END IF;
  END;

  PROCEDURE novo_produto(p_desc NVARCHAR2, p_cat NVARCHAR2, p_un NVARCHAR2, p_compra NUMBER,
                         p_venda NUMBER, p_qtde NUMBER, p_min NUMBER) IS
    v_n NUMBER; v_cat NUMBER; v_un NUMBER;
  BEGIN
    SELECT COUNT(*) INTO v_n FROM "PIM"."tb_produto" WHERE "descricao_produto" = p_desc;
    IF v_n = 0 THEN
      SELECT MIN("id_categoria") INTO v_cat FROM "PIM"."tb_categoria" WHERE UPPER("descricao") = UPPER(p_cat);
      SELECT MIN("id_unidade_medida") INTO v_un FROM "PIM"."tb_unidade_medida" WHERE "tipo" = p_un;
      INSERT INTO "PIM"."tb_produto" ("descricao_produto", "preco_compra_produto", "preco_venda_produto", "qdte_produto",
                                      "id_categoria", "estoque_minimo_produto", "id_unidade_medida")
        VALUES (p_desc, p_compra, p_venda, p_qtde, v_cat, p_min, v_un);
    END IF;
  END;
BEGIN
  novo_usuario(N'Gerente de Teste',  N'98765432100', N'Gerente',  3500, N'gerente',  N'Gerente');
  novo_usuario(N'Vendedor de Teste', N'45678912364', N'Vendedor', 2200, N'vendedor', N'Vendedor');

  novo_cliente(N'Maria Souza', N'52998224725', N'(18) 99111-0001', N'Rua das Flores', 120, N'Centro', N'19800-000');
  novo_cliente(N'João Pereira', N'11144477735', N'(18) 99111-0002', N'Av. Rui Barbosa', 455, N'Vila Operária', N'19800-100');
  novo_cliente(N'Ana Lima', N'39053344705', N'(18) 99111-0003', N'Rua Sete de Setembro', 78, N'Centro', N'19800-010');
  novo_cliente(N'Carlos Oliveira', N'12345678909', N'(18) 99111-0004', N'Rua Bahia', 300, N'Jardim Paulista', N'19806-200');
  novo_cliente(N'Fernanda Costa', N'16899535009', N'(18) 99111-0005', N'Rua Goiás', 12, N'Vila Glória', N'19806-300');
  novo_cliente(N'Paulo Santos', N'71428793860', N'(18) 99111-0006', N'Av. Dom Antônio', 990, N'Centro', N'19800-020');
  novo_cliente(N'Juliana Rocha', N'93541134780', N'(18) 99111-0007', N'Rua Paraná', 250, N'Vila Prudenciana', N'19806-100');
  novo_cliente(N'Roberto Alves', N'86288366757', N'(18) 99111-0008', N'Rua Minas Gerais', 64, N'Jardim Europa', N'19806-400');

  novo_fornecedor(N'Distribuidora Bom Preço', N'11222333000181', N'Alimentos', N'(18) 3322-1001', N'vendas@bompreco.teste', N'Ativo');
  novo_fornecedor(N'Bebidas Norte Atacado', N'11444777000161', N'Bebidas', N'(18) 3322-1002', N'pedidos@bebidasnorte.teste', N'Ativo');
  novo_fornecedor(N'Higiene e Limpeza Brasil', N'22233344000183', N'Higiene e limpeza', N'(18) 3322-1003', N'contato@hlbrasil.teste', N'Ativo');
  novo_fornecedor(N'Hortifruti Central', N'33344455000183', N'Hortifruti', N'(18) 3322-1004', N'central@hortifruti.teste', N'Ativo');
  novo_fornecedor(N'Atacado Antigo', N'44455566000183', N'Variedades', N'(18) 3322-1005', N'', N'Inativo');

  novo_produto(N'Refrigerante Cola 2L', N'Bebidas', N'UN', 6.50, 9.90, 48, 12);
  novo_produto(N'Refrigerante Guaraná 2L', N'Bebidas', N'UN', 6.20, 9.50, 36, 12);
  novo_produto(N'Água Mineral 500ml', N'Bebidas', N'UN', 0.90, 2.50, 120, 24);
  novo_produto(N'Suco de Laranja 1L', N'Bebidas', N'L', 4.00, 7.50, 30, 10);
  novo_produto(N'Cerveja Lata 350ml', N'Bebidas', N'UN', 2.80, 4.99, 96, 24);
  novo_produto(N'Energético Lata 250ml', N'Bebidas', N'UN', 5.50, 8.90, 8, 12);
  novo_produto(N'Arroz Branco 5kg', N'Mercearia', N'UN', 21.00, 29.90, 40, 10);
  novo_produto(N'Feijão Carioca 1kg', N'Mercearia', N'UN', 5.80, 8.99, 60, 15);
  novo_produto(N'Macarrão Espaguete 500g', N'Mercearia', N'UN', 2.60, 4.49, 80, 20);
  novo_produto(N'Açúcar Refinado 1kg', N'Mercearia', N'UN', 3.40, 5.49, 55, 15);
  novo_produto(N'Café Torrado 500g', N'Mercearia', N'UN', 12.00, 17.90, 25, 10);
  novo_produto(N'Óleo de Soja 900ml', N'Mercearia', N'UN', 5.90, 8.79, 5, 12);
  novo_produto(N'Leite Integral 1L', N'Mercearia', N'L', 3.90, 5.99, 72, 24);
  novo_produto(N'Biscoito Recheado 130g', N'Mercearia', N'UN', 1.70, 3.29, 90, 20);
  novo_produto(N'Farinha de Trigo 1kg', N'Mercearia', N'UN', 3.10, 4.99, 0, 10);
  novo_produto(N'Sabonete 85g', N'Higiene', N'UN', 1.10, 2.49, 100, 20);
  novo_produto(N'Shampoo 350ml', N'Higiene', N'UN', 8.50, 14.90, 18, 6);
  novo_produto(N'Papel Higiênico 12 rolos', N'Higiene', N'UN', 14.00, 21.90, 22, 8);
  novo_produto(N'Creme Dental 90g', N'Higiene', N'UN', 2.90, 5.49, 45, 10);
  novo_produto(N'Detergente 500ml', N'Limpeza', N'UN', 1.60, 2.99, 70, 20);
  novo_produto(N'Sabão em Pó 1kg', N'Limpeza', N'UN', 8.20, 12.90, 28, 8);
  novo_produto(N'Água Sanitária 1L', N'Limpeza', N'L', 2.70, 4.59, 6, 10);
  novo_produto(N'Banana Prata', N'Hortifruti', N'KG', 3.20, 5.99, 35, 10);
  novo_produto(N'Tomate', N'Hortifruti', N'KG', 4.50, 7.99, 20, 8);
  novo_produto(N'Cebola', N'Hortifruti', N'KG', 3.00, 5.49, 9, 10);
  COMMIT;
  DBMS_OUTPUT.PUT_LINE('ok: usuarios, clientes, fornecedores e produtos');
END;
/

-- 3. COMPRAS (feitas pelo gerente de teste; so cria se ele ainda nao tem nenhuma)
DECLARE
  v_gerente NUMBER;
  v_n       NUMBER;
  v_compra  NUMBER;

  PROCEDURE nova_compra(p_cnpj NVARCHAR2, p_status NVARCHAR2, p_dias NUMBER) IS
    v_forn NUMBER; v_status NUMBER;
  BEGIN
    SELECT "id_distribuidora" INTO v_forn FROM "PIM"."tb_distribuidora" WHERE "cnpj" = p_cnpj;
    SELECT "id_status_compra" INTO v_status FROM "PIM"."tb_status_compra" WHERE "descricao" = p_status;
    INSERT INTO "PIM"."tb_compra" ("data_hora_compra", "total_compra", "total_desconto_compra",
                                   "id_usuario", "id_distribuidora", "id_status_compra")
      VALUES (LOCALTIMESTAMP - NUMTODSINTERVAL(p_dias, 'DAY'), 0, 0, v_gerente, v_forn, v_status)
      RETURNING "id_compra" INTO v_compra;
  END;

  PROCEDURE item_compra(p_prod NVARCHAR2, p_qtde NUMBER, p_preco NUMBER, p_desc NUMBER) IS
    v_prod NUMBER; v_total NUMBER;
  BEGIN
    SELECT "id_produto" INTO v_prod FROM "PIM"."tb_produto" WHERE "descricao_produto" = p_prod;
    v_total := p_qtde * p_preco - p_desc;
    INSERT INTO "PIM"."tb_item_compra" ("qdte_item_compra", "preco_unitario_item_compra", "valor_desconto_item_compra",
                                        "valor_total_item_compra", "id_compra", "id_produto")
      VALUES (p_qtde, p_preco, p_desc, v_total, v_compra, v_prod);
    UPDATE "PIM"."tb_compra"
       SET "total_compra" = "total_compra" + v_total,
           "total_desconto_compra" = "total_desconto_compra" + p_desc
     WHERE "id_compra" = v_compra;
  END;
BEGIN
  SELECT "id_usuario" INTO v_gerente FROM "PIM"."tb_usuario" WHERE "username" = N'gerente';
  SELECT COUNT(*) INTO v_n FROM "PIM"."tb_compra" WHERE "id_usuario" = v_gerente;
  IF v_n = 0 THEN
    nova_compra(N'11444777000161', N'Concluido', 20);
    item_compra(N'Refrigerante Cola 2L', 48, 6.50, 0.00);
    item_compra(N'Refrigerante Guaraná 2L', 36, 6.20, 0.00);
    item_compra(N'Cerveja Lata 350ml', 96, 2.80, 0.00);
    nova_compra(N'11222333000181', N'Concluido', 12);
    item_compra(N'Arroz Branco 5kg', 40, 21.00, 0.00);
    item_compra(N'Feijão Carioca 1kg', 60, 5.80, 0.00);
    item_compra(N'Açúcar Refinado 1kg', 55, 3.40, 10.00);
    nova_compra(N'22233344000183', N'Concluido', 6);
    item_compra(N'Sabão em Pó 1kg', 28, 8.20, 0.00);
    item_compra(N'Detergente 500ml', 70, 1.60, 0.00);
    item_compra(N'Papel Higiênico 12 rolos', 22, 14.00, 0.00);
    nova_compra(N'11222333000181', N'Pendente', 1);
    item_compra(N'Óleo de Soja 900ml', 60, 5.90, 0.00);
    item_compra(N'Farinha de Trigo 1kg', 40, 3.10, 0.00);
    item_compra(N'Café Torrado 500g', 25, 12.00, 0.00);
  END IF;
  COMMIT;
  DBMS_OUTPUT.PUT_LINE('ok: compras');
END;
/

-- 4. VENDAS (60 dos ultimos 30 dias + 6 a prazo). So cria se os usuarios de teste ainda nao tem nenhuma.
DECLARE
  TYPE t_ids IS TABLE OF NUMBER INDEX BY PLS_INTEGER;
  v_prod     t_ids;            -- produtos de teste
  v_cli      t_ids;            -- clientes de teste, na ordem do cadastro
  v_tp       t_ids;            -- tipos de pagamento: 1 = Dinheiro, 2 = Pix, 3 = Cartao
  v_vendedor NUMBER;
  v_gerente  NUMBER;
  v_n        NUMBER;
  v_venda    NUMBER;
  v_user     NUMBER;
  v_cliente  NUMBER;

  FUNCTION f_produto(p_desc NVARCHAR2) RETURN NUMBER IS
    v_id NUMBER;
  BEGIN
    SELECT "id_produto" INTO v_id FROM "PIM"."tb_produto" WHERE "descricao_produto" = p_desc;
    RETURN v_id;
  END;

  -- p_desc NULL = sorteia (10% das vezes, R$ 1,00 de desconto no item)
  PROCEDURE item_venda(p_venda NUMBER, p_prod NUMBER, p_qtde NUMBER, p_desc NUMBER DEFAULT NULL) IS
    v_preco NUMBER; v_desc NUMBER; v_total NUMBER;
  BEGIN
    SELECT "preco_venda_produto" INTO v_preco FROM "PIM"."tb_produto" WHERE "id_produto" = p_prod;
    v_desc := NVL(p_desc, CASE WHEN DBMS_RANDOM.VALUE(0, 1) < 0.1 THEN LEAST(1, v_preco * p_qtde / 2) ELSE 0 END);
    v_total := v_preco * p_qtde - v_desc;
    INSERT INTO "PIM"."tb_item_venda" ("qdte_item_venda", "preco_unitario_item_venda", "valor_desconto_item_venda",
                                       "valor_total_item_venda", "id_venda", "id_produto")
      VALUES (p_qtde, v_preco, v_desc, v_total, p_venda, p_prod);
    UPDATE "PIM"."tb_venda"
       SET "total_venda" = "total_venda" + v_total,
           "total_desconto_venda" = "total_desconto_venda" + v_desc
     WHERE "id_venda" = p_venda;
  END;

  FUNCTION criar_venda(p_dias NUMBER, p_user NUMBER, p_cliente NUMBER) RETURN NUMBER IS
    v_id NUMBER;
  BEGIN
    INSERT INTO "PIM"."tb_venda" ("data_hora_venda", "total_venda", "total_desconto_venda", "status_venda_venda",
                                  "id_pessoa", "id_usuario")
      VALUES (LOCALTIMESTAMP - NUMTODSINTERVAL(p_dias, 'DAY')
                           - NUMTODSINTERVAL(TRUNC(DBMS_RANDOM.VALUE(0, 9)), 'HOUR')
                           - NUMTODSINTERVAL(TRUNC(DBMS_RANDOM.VALUE(0, 60)), 'MINUTE'),
              0, 0, N'Concluido', p_cliente, p_user)
      RETURNING "id_venda" INTO v_id;
    RETURN v_id;
  END;

  -- p_valor NULL = o total da venda; p_dias_atras NULL = na data da venda
  PROCEDURE pagar(p_venda NUMBER, p_tipo NUMBER, p_valor NUMBER DEFAULT NULL, p_dias_atras NUMBER DEFAULT NULL) IS
  BEGIN
    INSERT INTO "PIM"."tb_forma_pagamento" ("data_hora_pagamento", "total_forma_pagamento", "id_venda", "id_tipo_pagamento")
      SELECT CASE WHEN p_dias_atras IS NULL THEN "data_hora_venda" ELSE LOCALTIMESTAMP - NUMTODSINTERVAL(p_dias_atras, 'DAY') END,
             NVL(p_valor, "total_venda"), "id_venda", p_tipo
        FROM "PIM"."tb_venda" WHERE "id_venda" = p_venda;
  END;
BEGIN
  SELECT "id_usuario" INTO v_vendedor FROM "PIM"."tb_usuario" WHERE "username" = N'vendedor';
  SELECT "id_usuario" INTO v_gerente  FROM "PIM"."tb_usuario" WHERE "username" = N'gerente';
  SELECT COUNT(*) INTO v_n FROM "PIM"."tb_venda" WHERE "id_usuario" IN (v_vendedor, v_gerente);

  IF v_n = 0 THEN
    SELECT "id_produto" BULK COLLECT INTO v_prod FROM "PIM"."tb_produto"
     WHERE "descricao_produto" IN (
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
      N'Cebola')
     ORDER BY "id_produto";
    SELECT "id_pessoa" BULK COLLECT INTO v_cli FROM "PIM"."tb_pessoa"
     WHERE "cpf_pessoa" IN (N'52998224725', N'11144477735', N'39053344705', N'12345678909', N'16899535009', N'71428793860', N'93541134780', N'86288366757')
     ORDER BY "id_pessoa";
    SELECT "id_tipo_pagamento" INTO v_tp(1) FROM "PIM"."tb_tipo_pagamento" WHERE UPPER("descricao") = N'DINHEIRO';
    SELECT "id_tipo_pagamento" INTO v_tp(2) FROM "PIM"."tb_tipo_pagamento" WHERE UPPER("descricao") = N'PIX';
    SELECT "id_tipo_pagamento" INTO v_tp(3) FROM "PIM"."tb_tipo_pagamento" WHERE UPPER("descricao") = N'CARTÃO';

    DBMS_RANDOM.SEED(2026);   -- mesma sequencia sempre

    -- 60 vendas a vista nos ultimos 30 dias: 1 a 4 itens, 1 a 4 unidades de cada, pagamento sorteado
    FOR i IN 1 .. 60 LOOP
      v_user := CASE WHEN MOD(i, 3) = 0 THEN v_gerente ELSE v_vendedor END;
      v_cliente := CASE WHEN DBMS_RANDOM.VALUE(0, 1) < 0.4
                        THEN v_cli(TRUNC(DBMS_RANDOM.VALUE(1, v_cli.COUNT + 1))) ELSE NULL END;
      v_venda := criar_venda(TRUNC(DBMS_RANDOM.VALUE(0, 30)), v_user, v_cliente);
      FOR j IN 1 .. TRUNC(DBMS_RANDOM.VALUE(1, 5)) LOOP
        item_venda(v_venda, v_prod(TRUNC(DBMS_RANDOM.VALUE(1, v_prod.COUNT + 1))), TRUNC(DBMS_RANDOM.VALUE(1, 5)));
      END LOOP;
      pagar(v_venda, v_tp(TRUNC(DBMS_RANDOM.VALUE(1, 4))));
    END LOOP;

    -- vendas a prazo (modulo A prazo): a divida fica em tb_fiado e o saldo e o valor menos os pagamentos

    v_venda := criar_venda(10, v_vendedor, v_cli(1));   -- Maria Souza
    item_venda(v_venda, f_produto(N'Arroz Branco 5kg'), 1, 0);
    item_venda(v_venda, f_produto(N'Feijão Carioca 1kg'), 2, 0);
    INSERT INTO "PIM"."tb_fiado" ("id_venda", "id_pessoa", "valor_original", "data_hora_fiado")
      SELECT "id_venda", "id_pessoa", "total_venda", "data_hora_venda" FROM "PIM"."tb_venda" WHERE "id_venda" = v_venda;
    pagar(v_venda, v_tp(1), NULL, 8);   -- quitada
    v_venda := criar_venda(5, v_vendedor, v_cli(1));   -- Maria Souza
    item_venda(v_venda, f_produto(N'Leite Integral 1L'), 6, 0);
    item_venda(v_venda, f_produto(N'Café Torrado 500g'), 1, 0);
    INSERT INTO "PIM"."tb_fiado" ("id_venda", "id_pessoa", "valor_original", "data_hora_fiado")
      SELECT "id_venda", "id_pessoa", "total_venda", "data_hora_venda" FROM "PIM"."tb_venda" WHERE "id_venda" = v_venda;
    pagar(v_venda, v_tp(1), 30.00, 3);   -- pagamento parcial
    v_venda := criar_venda(2, v_vendedor, v_cli(1));   -- Maria Souza
    item_venda(v_venda, f_produto(N'Refrigerante Cola 2L'), 2, 0);
    item_venda(v_venda, f_produto(N'Biscoito Recheado 130g'), 3, 0);
    INSERT INTO "PIM"."tb_fiado" ("id_venda", "id_pessoa", "valor_original", "data_hora_fiado")
      SELECT "id_venda", "id_pessoa", "total_venda", "data_hora_venda" FROM "PIM"."tb_venda" WHERE "id_venda" = v_venda;
    v_venda := criar_venda(6, v_vendedor, v_cli(2));   -- João Pereira
    item_venda(v_venda, f_produto(N'Sabão em Pó 1kg'), 1, 0);
    item_venda(v_venda, f_produto(N'Detergente 500ml'), 3, 0);
    INSERT INTO "PIM"."tb_fiado" ("id_venda", "id_pessoa", "valor_original", "data_hora_fiado")
      SELECT "id_venda", "id_pessoa", "total_venda", "data_hora_venda" FROM "PIM"."tb_venda" WHERE "id_venda" = v_venda;
    v_venda := criar_venda(9, v_vendedor, v_cli(3));   -- Ana Lima
    item_venda(v_venda, f_produto(N'Papel Higiênico 12 rolos'), 1, 0);
    item_venda(v_venda, f_produto(N'Shampoo 350ml'), 1, 0);
    INSERT INTO "PIM"."tb_fiado" ("id_venda", "id_pessoa", "valor_original", "data_hora_fiado")
      SELECT "id_venda", "id_pessoa", "total_venda", "data_hora_venda" FROM "PIM"."tb_venda" WHERE "id_venda" = v_venda;
    pagar(v_venda, v_tp(1), 20.00, 4);   -- pagamento parcial
    v_venda := criar_venda(3, v_vendedor, v_cli(3));   -- Ana Lima
    item_venda(v_venda, f_produto(N'Cerveja Lata 350ml'), 6, 0);
    INSERT INTO "PIM"."tb_fiado" ("id_venda", "id_pessoa", "valor_original", "data_hora_fiado")
      SELECT "id_venda", "id_pessoa", "total_venda", "data_hora_venda" FROM "PIM"."tb_venda" WHERE "id_venda" = v_venda;
  END IF;
  COMMIT;
  DBMS_OUTPUT.PUT_LINE('ok: vendas');
END;
/

-- 5. CONFERENCIA
SELECT 'produtos' AS tabela, COUNT(*) AS qtd FROM "PIM"."tb_produto"
UNION ALL SELECT 'clientes (dados de teste)', COUNT(*) FROM "PIM"."tb_pessoa" WHERE "observacao_pessoa" = N'DADOS DE TESTE'
UNION ALL SELECT 'fornecedores', COUNT(*) FROM "PIM"."tb_distribuidora"
UNION ALL SELECT 'compras', COUNT(*) FROM "PIM"."tb_compra"
UNION ALL SELECT 'vendas', COUNT(*) FROM "PIM"."tb_venda"
UNION ALL SELECT 'vendas a prazo', COUNT(*) FROM "PIM"."tb_fiado";

-- produtos abaixo do estoque minimo (devem aparecer no alerta de estoque baixo)
SELECT "descricao_produto", "qdte_produto", "estoque_minimo_produto"
  FROM "PIM"."tb_produto" WHERE "qdte_produto" <= "estoque_minimo_produto" ORDER BY 2;

