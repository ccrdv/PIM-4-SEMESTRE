import 'package:flutter/foundation.dart';
import '../../models/Item_Venda.dart';
import '../api/api.dart';
import '../db/db.dart';

class Itens_Service {
  // Baixa as vendas da API e grava os itens de cada venda no banco local.
  static Future<void> sincronizarVendas() async {
    try {
      // Cada venda já vem da API com a lista de itens dentro.
      final vendas = await ApiConnection.getLista('/api/Venda');
      final db = await Db.instance.database;

      // Transação: ou grava todos os itens, ou nenhum.
      await db.transaction((txn) async {
        for (final venda in vendas) {
          // Id da venda e lista de itens, procurados no JSON.
          final idVenda = ApiConnection.valor(venda, ['idVenda']);
          final itens = ApiConnection.valor(venda, ['itens']);
          // Venda sem id ou sem lista de itens é ignorada.
          if (idVenda == null || itens is! List) continue;

          // Apaga os itens antigos da venda para regravar sem duplicar.
          await txn.delete(
            'tbl_Item_Venda',
            where: 'Id_Venda = ?',
            whereArgs: [idVenda],
          );

          // Insere cada item da venda no banco local.
          for (final item in itens) {
            final i = Map<String, dynamic>.from(item as Map);
            await txn.insert('tbl_Item_Venda', {
              // Cada coluna local recebe o campo correspondente do JSON.
              'QDTE_Item_Venda': ApiConnection.valor(i, ['quantidade']),
              'Preco_Unitario_Item_Venda':
              ApiConnection.valor(i, ['precoUnitario']),
              'Valor_Desconto_Item_Venda':
              ApiConnection.valor(i, ['valorDesconto']),
              'Valor_Total_Item_Venda': ApiConnection.valor(i, ['valorTotal']),
              'Id_Venda': idVenda,
              'Id_Produto': ApiConnection.valor(i, ['produtoId']),
            });
          }
        }
      });
    } catch (e) {
      // Qualquer falha só é registrada no log; o app segue funcionando.
      debugPrint('Erro ao sincronizar itens de venda: $e');
    }
  }

  // Lê todos os itens de venda guardados no celular.
  static Future<List<ItemVenda>> itensVenda() async {
    final db = await Db.instance.database;
    final mapas = await db.query('tbl_Item_Venda');
    return mapas.map((m) => ItemVenda.fromMap(m)).toList();
  }

}