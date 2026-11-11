import 'package:flutter/foundation.dart';
import 'package:sqflite/sqflite.dart';
import '../../models/Venda.dart';
import '../api/dados.dart';
import '../api/itens_service.dart';
import '../db/db.dart';

class Venda_Reposity extends ChangeNotifier {
  static const String _endpoint = '/api/Venda';
  static const Map<String, List<String>> _chaves = {
    'Id_Venda': ['idVenda', 'id'],
    'Data_Hora_Venda': ['dataHoraVenda', 'dataHora'],
    'Total_Venda': ['totalVenda', 'total'],
    'Total_Desconto_Venda': ['totalDescontoVenda', 'totalDesconto'],
  };

  late Database db;
  List<Venda> _vendas = [];

  List<Venda> get vendas => _vendas;

  Venda_Reposity() {
    _initRepository();
  }

  Future<void> _initRepository() async {
    await _carregarLocal();
    try {
      await atualizarDaApi();
    } catch (e) {
      debugPrint('Erro ao sincronizar vendas: $e');
    }
  }

  Future<void> _carregarLocal() async {
    db = await Db.instance.database;
    final mapas = await db.query('tbl_Venda');
    _vendas = mapas.map((m) => Venda.fromMap(m)).toList();
    _vendas.sort((a, b) => b.dataHora.compareTo(a.dataHora));
    notifyListeners();
  }

  Future<void> atualizarDaApi() async {
    db = await Db.instance.database;
    await Dados.sincronizar(db, 'tbl_Venda', _endpoint, _chaves);
    await Itens_Service.sincronizarVendas();
    await _carregarLocal();
  }
}