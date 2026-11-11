import 'package:flutter/widgets.dart';
import 'package:provider/provider.dart';
import '../repositories/Categoria_Repository.dart';
import '../repositories/Produto_Repository.dart';
import '../repositories/Venda_Repository.dart';

class Sincronizador {
  // Atualiza categorias, produtos, vendas e compras a partir da API, todos ao mesmo tempo.
  static Future<void> tudo(BuildContext context) async {
    // Pega os 3 repositórios que foram criados no main.dart.
    final categorias = context.read<Categoria_Reposity>();
    final produtos = context.read<Produto_Reposity>();
    final vendas = context.read<Venda_Reposity>();

    // Roda as 4 atualizações em paralelo e espera todas terminarem.
    await Future.wait([
      // O catchError vazio engole a falha para uma não derrubar as outras.
      categorias.atualizarDaApi().catchError((_) {}),
      produtos.atualizarDaApi().catchError((_) {}),
      vendas.atualizarDaApi().catchError((_) {}),
    ]);
  }
}