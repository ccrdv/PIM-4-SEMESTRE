import 'dart:convert';
import 'dart:io' show Platform;
import 'package:flutter/foundation.dart';
import 'api.dart';
import 'sessao.dart';

// Resultados possíveis do pareamento: deu certo, QR/código inválido ou sem conexão.
enum ResultadoLogin { ok, invalido, semConexao }

class Auth_Service {
  // Converte um texto JSON em Map; devolve null se não for JSON válido.
  static Map<String, dynamic>? _corpo(String texto) {
    try {
      final d = jsonDecode(texto);
      return d is Map ? Map<String, dynamic>.from(d) : null;
    } catch (_) {
      return null;
    }
  }

  // Nome do aparelho enviado à API ao parear.
  static String _nomeDispositivo() {
    if (Platform.isAndroid) return 'Celular Android';
    if (Platform.isIOS) return 'iPhone';
    return Platform.operatingSystem;
  }

  // Pergunta à API se o token ainda vale: true = válido, false = 401, null = não deu para saber.
  static Future<bool?> validarSessao() async {
    // Sem token não há sessão para validar.
    if (ApiConnection.token == null) return false;
    try {
      final r = await ApiConnection.get('/api/Dispositivo/minha-sessao');
      // Qualquer 2xx significa sessão válida.
      if (r.statusCode >= 200 && r.statusCode < 300) return true;
      // 401 significa token inválido ou expirado.
      if (r.statusCode == 401) return false;
      // Outros status (500, etc.): não dá para concluir nada.
      return null;
    } catch (_) {
      // Sem conexão ou timeout: resultado desconhecido.
      return null;
    }
  }

  // Faz o pareamento a partir do texto lido no QR code.
  static Future<ResultadoLogin> parear(String qr) async {
    // O QR traz um JSON com o endereço da API e um código de pareamento.
    final dadosQr = _corpo(qr);
    final api =
    dadosQr == null ? null : ApiConnection.valor(dadosQr, ['api'])?.toString();
    final codigo = dadosQr == null
        ? null
        : ApiConnection.valor(dadosQr, ['codigo'])?.toString();

    // QR sem api ou sem código é inválido.
    if (api == null || api.trim().isEmpty || codigo == null || codigo.isEmpty) {
      return ResultadoLogin.invalido;
    }

    // Guarda o estado atual para desfazer se o pareamento falhar.
    final baseAnterior = ApiConnection.baseUrl;
    final tokenAnterior = ApiConnection.token;

    // Volta a URL e o token para o que eram antes da tentativa.
    void restaurar() {
      ApiConnection.baseUrl = baseAnterior;
      ApiConnection.token = tokenAnterior;
    }

    // Aponta para a API do QR e zera o token para a chamada de pareamento.
    ApiConnection.baseUrl = ApiConnection.normalizarBase(api);
    ApiConnection.token = null;

    try {
      // Envia o código e o nome do aparelho para a API.
      final r = await ApiConnection.post(
        '/api/Dispositivo/parear',
        {'codigo': codigo, 'nomeDispositivo': _nomeDispositivo()},
      );

      // Resposta fora de 2xx: código recusado.
      if (r.statusCode < 200 || r.statusCode >= 300) {
        restaurar();
        return ResultadoLogin.invalido;
      }

      // Lê o corpo da resposta e procura o token de acesso.
      final dados = _corpo(utf8.decode(r.bodyBytes));
      final access =
      dados == null ? null : ApiConnection.valor(dados, ['accessToken']);
      if (dados == null || access == null) {
        restaurar();
        return ResultadoLogin.invalido;
      }

      // Data de validade do token (pode não vir).
      final expira = ApiConnection.valor(dados, ['expiraEm']);

      // Guarda API, token e validade no aparelho.
      await Sessao.salvar(
        api: ApiConnection.baseUrl!,
        token: access.toString(),
        expiraEm: expira == null ? null : DateTime.tryParse(expira.toString()),
      );
      return ResultadoLogin.ok;
    } catch (e) {
      // Timeout ou falha de rede: desfaz e avisa que faltou conexão.
      debugPrint('Erro ao parear: $e');
      restaurar();
      return ResultadoLogin.semConexao;
    }
  }
}