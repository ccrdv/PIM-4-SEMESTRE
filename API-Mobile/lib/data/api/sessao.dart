import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'api.dart';

class Sessao {
  // Nomes das chaves usadas para guardar cada dado no aparelho.
  static const String _chaveApi = 'sessao_api';
  static const String _chaveToken = 'sessao_token';
  static const String _chaveExpiraEm = 'sessao_expira_em';

  // Armazenamento criptografado, usado só para o token.
  static const FlutterSecureStorage _seguro = FlutterSecureStorage();

  // Data em que o token expira (pode ser nula).
  static DateTime? expiraEm;

  // Há sessão se existem API e token e a validade não passou.
  static bool get existe =>
      (ApiConnection.baseUrl ?? '').isNotEmpty &&
          (ApiConnection.token ?? '').isNotEmpty &&
          !expirada;

  // Expirada só se há data de validade e ela já passou.
  static bool get expirada =>
      expiraEm != null && DateTime.now().isAfter(expiraEm!);

  // Lê a sessão guardada e preenche o ApiConnection ao abrir o app.
  static Future<void> carregar() async {
    final prefs = await SharedPreferences.getInstance();
    // API e validade ficam no SharedPreferences (não são segredos).
    ApiConnection.baseUrl = prefs.getString(_chaveApi);
    expiraEm = DateTime.tryParse(prefs.getString(_chaveExpiraEm) ?? '');

    // O token vem do armazenamento seguro.
    var token = await _seguro.read(key: _chaveToken);

    // Migração: se havia token antigo em texto puro, passa para o seguro.
    final antigo = prefs.getString(_chaveToken);
    if (antigo != null) {
      if (token == null || token.isEmpty) {
        token = antigo;
        await _seguro.write(key: _chaveToken, value: antigo);
      }
      // Remove a cópia insegura de qualquer jeito.
      await prefs.remove(_chaveToken);
    }

    ApiConnection.token = token;
  }

  // Guarda a sessão no aparelho e na memória após o pareamento.
  static Future<void> salvar({
    required String api,
    required String token,
    DateTime? expiraEm,
  }) async {
    final prefs = await SharedPreferences.getInstance();

    // Atualiza a memória para uso imediato.
    ApiConnection.baseUrl = api;
    ApiConnection.token = token;
    Sessao.expiraEm = expiraEm;

    // API no SharedPreferences e token no armazenamento seguro.
    await prefs.setString(_chaveApi, api);
    await _seguro.write(key: _chaveToken, value: token);
    // Sem validade informada, apaga qualquer data antiga.
    if (expiraEm != null) {
      await prefs.setString(_chaveExpiraEm, expiraEm.toIso8601String());
    } else {
      await prefs.remove(_chaveExpiraEm);
    }
  }

  // Apaga a sessão da memória e do aparelho (logout ou 401).
  static Future<void> limpar() async {
    final prefs = await SharedPreferences.getInstance();

    ApiConnection.baseUrl = null;
    ApiConnection.token = null;
    expiraEm = null;

    await prefs.remove(_chaveApi);
    // Remove também a chave antiga do token, por segurança.
    await prefs.remove(_chaveToken);
    await prefs.remove(_chaveExpiraEm);
    await _seguro.delete(key: _chaveToken);
  }
}