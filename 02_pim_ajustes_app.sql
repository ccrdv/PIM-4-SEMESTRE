-- =====================================================================
-- SistemaWhiteLabel - ajustes do banco exigidos pelo app (parte 2)
-- Rode conectado como PIM, DEPOIS do 01_pim_corrections.sql.
-- =====================================================================
SET DEFINE OFF;

-- 1. ENDERECO: o app guarda um endereço em texto livre (como antes),
--    entao o logradouro cabe 150 caracteres e os demais campos sao opcionais.
--    (Se voce ja rodou o bloco OPTIONAL A do script 01, o segundo comando
--    dara ORA-01451; pode ignorar.)
ALTER TABLE "PIM"."tbl_Endereco" MODIFY ("Logradouro" NVARCHAR2(150));
ALTER TABLE "PIM"."tbl_Endereco" MODIFY ("Id_Municipio" NULL, "Bairro" NULL, "CEP" NULL, "Numero" NULL);

-- 2. USUARIO INICIAL PARA TESTES (o banco nao tem nenhum login).
--    O app compara a senha em texto puro: TROQUE 'admin123' depois do primeiro acesso.
INSERT INTO "PIM"."tbl_Pessoa" ("Nome_Pessoa", "CPF_Pessoa") VALUES (N'Administrador', N'00000000000');

INSERT INTO "PIM"."tbl_Funcionario" ("Id_Pessoa", "Cargo", "Salario")
VALUES ((SELECT "Id_Pessoa" FROM "PIM"."tbl_Pessoa" WHERE "CPF_Pessoa" = N'00000000000'), N'Gerente', 0);

-- Id_Perfil 1 = Administrador (veja a ordem do seed no script 01)
INSERT INTO "PIM"."tbl_Usuario" ("UserName", "Senha", "Status", "Id_Pessoa", "Id_Perfil")
VALUES (N'admin', N'admin123', N'Ativo',
        (SELECT "Id_Pessoa" FROM "PIM"."tbl_Pessoa" WHERE "CPF_Pessoa" = N'00000000000'), 1);
COMMIT;

-- 3. CONFERENCIA: o produto usa Id_Unidade_Medida = 1 por padrao (deve ser 'UN')
SELECT "Id_Unidade_Medida", "Tipo" FROM "PIM"."tbl_Unidade_Medida" ORDER BY 1;
