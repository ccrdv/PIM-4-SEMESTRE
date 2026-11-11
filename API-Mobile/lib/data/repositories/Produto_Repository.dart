import 'dart:convert';
import 'dart:math';
import 'package:flutter/foundation.dart';
import 'package:sqflite/sqflite.dart';
import '../../models/Produto.dart';
import '../api/api.dart';
import '../api/dados.dart';
import '../db/db.dart';

// Resumo de um envio: quantos foram, quantos faltam, quantos deram erro e se houve 403.
class ResultadoEnvio {
  final int enviados;
  final int pendentes;
  final int comErro;
  final bool semPermissao;

  const ResultadoEnvio({
    this.enviados = 0,
    this.pendentes = 0,
    this.comErro = 0,
    this.semPermissao = false,
  });
}

// Guarda a lista de produtos, avisa as telas quando muda e cuida do envio para a API.
class Produto_Reposity extends ChangeNotifier {
  // Rota da API de produtos.
  static const String _endpoint = '/api/Produto';
  // Texto gravado na coluna Erro quando a API responde 403.
  static const String _erroSemPermissao = 'SEM_PERMISSAO';
  // Coluna local -> nomes possíveis do campo no JSON da API.
  static const Map<String, List<String>> _chaves = {
    'Id_Produto': ['idProduto', 'id'],
    'Descricao_Produto': ['descricao'],
    'Preco_Compra_Produto': ['precoCompra', 'Preco_Compra'],
    'preco_Venda_Produto': ['Preco_Venda_Produto', 'precoVenda'],
    'QDTE_Produto': ['qtde', 'quantidade'],
    'Id_Categoria': ['idCategoria'],
  };

  // Conexão com o banco local.
  late Database db;
  // Lista em memória que as telas observam.
  List<Produto> _produtos = [];
  // Trava para não enviar duas vezes ao mesmo tempo.
  bool _enviando = false;

  // Acesso somente leitura à lista de produtos.
  List<Produto> get produtos => _produtos;

  // Ao criar o repositório, já inicia a carga e a sincronização.
  Produto_Reposity() {
    _initRepository();
  }

  // Carrega o local, envia pendentes e baixa da API (falhas só vão para o log).
  Future<void> _initRepository() async {
    await _carregarLocal();
    try {
      await enviarPendentes();
      await atualizarDaApi();
    } catch (e) {
      debugPrint('Erro ao sincronizar produtos: $e');
    }
  }

  // Lê os produtos do banco local e avisa as telas.
  Future<void> _carregarLocal() async {
    db = await Db.instance.database;
    final mapas = await db.query('tbl_Produto');
    _produtos = mapas.map((m) => Produto.fromMap(m)).toList();
    notifyListeners();
  }

  // Versão pública de _carregarLocal (hoje sem uso externo).
  Future<void> recarregar() => _carregarLocal();

  // Baixa os produtos da API, apaga os que sumiram do servidor e recarrega.
  Future<void> atualizarDaApi() async {
    db = await Db.instance.database;
    await Dados.sincronizar(
      db,
      'tbl_Produto',
      _endpoint,
      _chaves,
      // Remove do celular o que não veio da API, menos os pendentes.
      removerAusentes: true,
      temPendente: true,
    );
    await _carregarLocal();
  }

  // Gera um UUID v4 que a API pode usar para não duplicar o produto.
  String _novoClienteId() {
    final r = Random.secure();
    final b = List<int>.generate(16, (_) => r.nextInt(256));
    // Marca a versão 4 e a variante do UUID.
    b[6] = (b[6] & 0x0f) | 0x40;
    b[8] = (b[8] & 0x3f) | 0x80;
    final h = b.map((x) => x.toRadixString(16).padLeft(2, '0')).join();
    return '${h.substring(0, 8)}-${h.substring(8, 12)}-'
        '${h.substring(12, 16)}-${h.substring(16, 20)}-${h.substring(20)}';
  }

  // Salva o produto no celular como pendente e tenta enviar na hora.
  Future<ResultadoEnvio> adicionar(Produto produto) async {
    db = await Db.instance.database;

    // Acha o menor id atual para criar um id negativo provisório.
    final menor = Sqflite.firstIntValue(
      await db.rawQuery('SELECT MIN(Id_Produto) FROM tbl_Produto'),
    ) ??
        0;
    final idProvisorio = (menor < 0 ? menor : 0) - 1;

    // Grava o produto com Pendente = 1 e um Cliente_Id novo.
    await db.insert('tbl_Produto', {
      'Id_Produto': idProvisorio,
      'Descricao_Produto': produto.descricao,
      'Preco_Compra_Produto': produto.precoCompra,
      'preco_Venda_Produto': produto.precoVenda,
      'QDTE_Produto': produto.qtde,
      'Id_Categoria': produto.idCategoria,
      'Pendente': 1,
      'Cliente_Id': _novoClienteId(),
    });

    // Mostra o produto na lista e já tenta enviar.
    await _carregarLocal();
    return enviarPendentes();
  }

  // Envia todos os produtos pendentes para a API e devolve o resumo.
  Future<ResultadoEnvio> enviarPendentes() async {
    // Se já está enviando, não faz nada.
    if (_enviando) return const ResultadoEnvio();
    _enviando = true;

    var enviados = 0;
    var semPermissao = false;

    try {
      db = await Db.instance.database;
      // Libera os produtos com erro para serem tentados de novo.
      await db.update(
        'tbl_Produto',
        {'Erro': null},
        where: 'Pendente = 1 AND Erro IS NOT NULL',
      );
      // Busca os pendentes, dos ids negativos mais antigos para os mais novos.
      final lista = await db.query(
        'tbl_Produto',
        where: 'Pendente = 1 AND Erro IS NULL',
        orderBy: 'Id_Produto DESC',
      );

      for (final p in lista) {
        final id = p['Id_Produto'];

        // Produtos antigos podem não ter Cliente_Id: cria e grava um agora.
        var clienteId = p['Cliente_Id'] as String?;
        if (clienteId == null || clienteId.isEmpty) {
          clienteId = _novoClienteId();
          await db.update(
            'tbl_Produto',
            {'Cliente_Id': clienteId},
            where: 'Id_Produto = ?',
            whereArgs: [id],
          );
        }

        try {
          // Envia o produto; o mesmo clienteId é reenviado se houver nova tentativa.
          final r = await ApiConnection.post(_endpoint, {
            'clienteId': clienteId,
            'descricao': p['Descricao_Produto'],
            'precoCompra': p['Preco_Compra_Produto'],
            'precoVenda': p['preco_Venda_Produto'],
            'qtde': p['QDTE_Produto'],
            'idCategoria': p['Id_Categoria'],
          });

          if (r.statusCode >= 200 && r.statusCode < 300) {
            // Sucesso: apaga a cópia local (a oficial vem no próximo download).
            await db.delete(
              'tbl_Produto',
              where: 'Id_Produto = ?',
              whereArgs: [id],
            );
            enviados++;
          } else if (r.statusCode == 403) {
            // Sem permissão: mantém o produto e marca o motivo.
            await db.update(
              'tbl_Produto',
              {'Erro': _erroSemPermissao},
              where: 'Id_Produto = ?',
              whereArgs: [id],
            );
            semPermissao = true;
          } else if (r.statusCode == 401 || r.statusCode >= 500) {
            // Sessão caiu ou servidor com problema: para e tenta depois.
            break;
          } else {
            // Outro 4xx: guarda o início da resposta como motivo do erro.
            final corpo = utf8.decode(r.bodyBytes, allowMalformed: true);
            final trecho =
            corpo.length > 120 ? corpo.substring(0, 120) : corpo;
            await db.update(
              'tbl_Produto',
              {'Erro': 'HTTP ${r.statusCode} $trecho'.trim()},
              where: 'Id_Produto = ?',
              whereArgs: [id],
            );
          }
        } catch (_) {
          // Timeout ou sem conexão: para e mantém pendente.
          break;
        }
      }

      // Se algo foi enviado, baixa a lista oficial; senão só recarrega o local.
      if (enviados > 0) {
        try {
          await atualizarDaApi();
        } catch (e) {
          debugPrint('Erro ao atualizar após envio: $e');
          await _carregarLocal();
        }
      } else {
        await _carregarLocal();
      }

      // Conta os que ainda aguardam envio.
      final restantes = Sqflite.firstIntValue(
        await db.rawQuery(
          'SELECT COUNT(*) FROM tbl_Produto '
              'WHERE Pendente = 1 AND Erro IS NULL',
        ),
      ) ??
          0;
      // Conta os que a API recusou nesta rodada.
      final comErro = Sqflite.firstIntValue(
        await db.rawQuery(
          'SELECT COUNT(*) FROM tbl_Produto '
              'WHERE Pendente = 1 AND Erro IS NOT NULL',
        ),
      ) ??
          0;

      return ResultadoEnvio(
        enviados: enviados,
        pendentes: restantes,
        comErro: comErro,
        semPermissao: semPermissao,
      );
    } finally {
      // Libera a trava, mesmo se der erro.
      _enviando = false;
    }
  }
}