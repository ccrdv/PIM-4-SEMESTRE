import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../models/Produto.dart';
import '../../data/repositories/Categoria_Repository.dart';
import '../../data/repositories/Produto_Repository.dart';


// Tela com o formulário para cadastrar um produto novo.
class Cadastro extends StatefulWidget {
  const Cadastro({super.key});

  @override
  State<Cadastro> createState() => _CadastroState();
}

class _CadastroState extends State<Cadastro> {
  // Chave usada para validar o formulário inteiro de uma vez.
  final _formKey = GlobalKey<FormState>();
  // Controladores que guardam o texto digitado em cada campo.
  final _nomeController = TextEditingController();
  final _precoCompraController = TextEditingController();
  final _precoVendaController = TextEditingController();
  final _qtdeController = TextEditingController();

  // Id da categoria escolhida no dropdown (nulo = nenhuma).
  int? _categoriaSelecionada;

  // Libera a memória dos controladores ao sair da tela.
  @override
  void dispose() {
    _nomeController.dispose();
    _precoCompraController.dispose();
    _precoVendaController.dispose();
    _qtdeController.dispose();
    super.dispose();
  }

  // Valida o formulário, salva o produto e mostra o resultado.
  Future<void> _salvar() async {
    // Se algum campo estiver inválido, para aqui.
    if (!_formKey.currentState!.validate()) return;
    // Segunda checagem da categoria, com aviso em snackbar.
    if (_categoriaSelecionada == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Selecione uma categoria')),
      );
      return;
    }

    // Monta o produto; troca vírgula por ponto para o double funcionar.
    final produto = Produto(
      descricao: _nomeController.text.trim(),
      precoCompra: double.parse(_precoCompraController.text.replaceAll(',', '.')),
      precoVenda: double.parse(_precoVendaController.text.replaceAll(',', '.')),
      qtde: int.parse(_qtdeController.text),
      idCategoria: _categoriaSelecionada!,
    );

    // Salva no celular e tenta enviar para a API.
    final repo = context.read<Produto_Reposity>();
    final resultado = await repo.adicionar(produto);

    // Se a tela foi fechada durante o envio, não mexe mais nela.
    if (!mounted) return;

    // 403: avisa em vermelho e deixa o formulário preenchido.
    if (resultado.semPermissao) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Você não tem permissão para cadastrar produtos. Apenas Administrador e Gerente podem.'),
          backgroundColor: Color(0xFFDC2626),
        ),
      );
      return;
    }

    // Laranja se ficou pendente (sem conexão); verde se foi enviado.
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          resultado.pendentes > 0
              ? 'Produto salvo no aparelho. Será enviado quando houver conexão.'
              : 'Produto cadastrado',
        ),
        backgroundColor: resultado.pendentes > 0
            ? const Color(0xFFF59E0B)
            : const Color(0xFF16A34A),
      ),
    );
    // Limpa o formulário para o próximo cadastro.
    _nomeController.clear();
    _precoCompraController.clear();
    _precoVendaController.clear();
    _qtdeController.clear();
    setState(() => _categoriaSelecionada = null);
  }

  @override
  Widget build(BuildContext context) {
    // watch: o dropdown se atualiza quando as categorias mudam.
    final categorias = context.watch<Categoria_Reposity>().categorias;

    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      body: SafeArea(
        // Permite rolar a tela quando o teclado ocupa espaço.
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(20),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Título e subtítulo da tela.
                const Text(
                  'Cadastrar produto',
                  style: TextStyle(
                    fontSize: 28,
                    fontWeight: FontWeight.bold,
                    color: Color(0xFF1E293B),
                  ),
                ),
                const SizedBox(height: 4),
                const Text(
                  'Preencha os dados do novo produto.',
                  style: TextStyle(color: Colors.grey, fontSize: 14),
                ),
                const SizedBox(height: 28),
                // Campo do nome (obrigatório).
                _Campo(
                  label: 'Nome',
                  controller: _nomeController,
                  hint: 'Ex: Produto 4',
                  validator: (v) =>
                  v == null || v.trim().isEmpty ? 'Informe o nome' : null,
                ),
                const SizedBox(height: 16),
                const Text(
                  'Categoria',
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w500,
                    color: Color(0xFF475569),
                  ),
                ),
                const SizedBox(height: 6),
                // Dropdown com as categorias vindas do repositório.
                DropdownButtonFormField<int>(
                  value: _categoriaSelecionada,
                  decoration: InputDecoration(
                    filled: true,
                    fillColor: Colors.white,
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(12),
                      borderSide: BorderSide.none,
                    ),
                    contentPadding: const EdgeInsets.symmetric(
                        horizontal: 16, vertical: 14),
                  ),
                  hint: const Text('Selecione'),
                  // Cada categoria vira uma opção: valor = id, texto = descrição.
                  items: categorias
                      .map((c) => DropdownMenuItem(
                    value: c.id,
                    child: Text(c.descricao),
                  ))
                      .toList(),
                  onChanged: (v) => setState(() => _categoriaSelecionada = v),
                  validator: (v) => v == null ? 'Selecione uma categoria' : null,
                ),
                const SizedBox(height: 16),
                // Preço de compra: aceita vírgula ou ponto.
                _Campo(
                  label: 'Preço de compra',
                  controller: _precoCompraController,
                  hint: 'R\$ 0,00',
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  validator: (v) {
                    if (v == null || v.isEmpty) return 'Informe o preço';
                    if (double.tryParse(v.replaceAll(',', '.')) == null) {
                      return 'Valor inválido';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),
                // Preço de venda: mesma validação do preço de compra.
                _Campo(
                  label: 'Preço de venda',
                  controller: _precoVendaController,
                  hint: 'R\$ 0,00',
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  validator: (v) {
                    if (v == null || v.isEmpty) return 'Informe o preço';
                    if (double.tryParse(v.replaceAll(',', '.')) == null) {
                      return 'Valor inválido';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),
                // Quantidade: só número inteiro.
                _Campo(
                  label: 'Quantidade inicial',
                  controller: _qtdeController,
                  hint: '0',
                  keyboardType: TextInputType.number,
                  validator: (v) {
                    if (v == null || v.isEmpty) return 'Informe a quantidade';
                    if (int.tryParse(v) == null) return 'Número inválido';
                    return null;
                  },
                ),
                const SizedBox(height: 32),
                // Botão que chama o _salvar.
                SizedBox(
                  width: double.infinity,
                  height: 52,
                  child: ElevatedButton(
                    onPressed: _salvar,
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFF1E3A8A),
                      foregroundColor: Colors.white,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                      elevation: 0,
                    ),
                    child: const Text(
                      'Salvar produto',
                      style: TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

// Campo de texto reutilizável: rótulo em cima e caixa de texto embaixo.
class _Campo extends StatelessWidget {
  final String label;
  final TextEditingController controller;
  final String hint;
  final TextInputType? keyboardType;
  final String? Function(String?)? validator;

  const _Campo({
    required this.label,
    required this.controller,
    required this.hint,
    this.keyboardType,
    this.validator,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Rótulo do campo.
        Text(
          label,
          style: const TextStyle(
            fontSize: 13,
            fontWeight: FontWeight.w500,
            color: Color(0xFF475569),
          ),
        ),
        const SizedBox(height: 6),
        // Caixa de texto com fundo branco e bordas arredondadas.
        TextFormField(
          controller: controller,
          keyboardType: keyboardType,
          validator: validator,
          decoration: InputDecoration(
            hintText: hint,
            filled: true,
            fillColor: Colors.white,
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
              borderSide: BorderSide.none,
            ),
            contentPadding:
            const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
          ),
        ),
      ],
    );
  }
}