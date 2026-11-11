import 'package:flutter/foundation.dart';
import 'package:sqflite/sqflite.dart';
import '../../models/Categoria.dart';
import '../api/dados.dart';
import '../db/db.dart';

class Categoria_Reposity extends ChangeNotifier {
  static const String _endpoint = '/api/Categoria';
  static const Map<String, List<String>> _chaves = {
    'Id_Categoria': ['idCategoria', 'id'],
    'Descricao_Categoria': ['descricao'],
  };

  late Database db;
  List<Categoria> _categorias = [];

  List<Categoria> get categorias => _categorias;

  Categoria_Reposity() {
    _initRepository();
  }

  Future<void> _initRepository() async {
    await _carregarLocal();
    try {
      await atualizarDaApi();
    } catch (e) {
      debugPrint('Erro ao sincronizar categorias: $e');
    }
  }

  Future<void> _carregarLocal() async {
    db = await Db.instance.database;
    final mapas = await db.query('tbl_Categoria');
    _categorias = mapas.map((m) => Categoria.fromMap(m)).toList();
    notifyListeners();
  }

  Future<void> atualizarDaApi() async {
    db = await Db.instance.database;
    await Dados.sincronizar(db, 'tbl_Categoria', _endpoint, _chaves);
    await _carregarLocal();
  }
}