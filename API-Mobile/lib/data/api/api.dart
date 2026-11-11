import 'dart:convert';
import 'package:http/http.dart' as http;

// Exceção para quando o usuário não tem permissão (não é usada no momento).
class SemPermissao implements Exception {
  const SemPermissao();

  @override
  String toString() => 'Sem permissão para esta ação';
}

class ApiConnection {
  // Endereço base da API, preenchido ao ler o QR code.
  static String? baseUrl;

  // Token de acesso enviado em todas as chamadas.
  static String? token;

  // Função chamada quando a API responde 401 (ligada ao encerrarSessao).
  static void Function()? aoNaoAutorizado;

  // Limpa a URL: tira as barras finais e o "/api" do fim.
  static String normalizarBase(String api) {
    var base = api.trim();
    // Remove todas as barras no final.
    while (base.endsWith('/')) {
      base = base.substring(0, base.length - 1);
    }
    // Remove o "/api" final para não duplicar nos endpoints.
    if (base.toLowerCase().endsWith('/api')) {
      base = base.substring(0, base.length - 4);
    }
    return base;
  }

  // Monta a URL completa; dá erro se o aparelho não foi pareado.
  static Uri _url(String endpoint) {
    final base = baseUrl;
    if (base == null || base.isEmpty) {
      throw StateError('Aparelho não pareado: falta ler o QR code.');
    }
    return Uri.parse('$base$endpoint');
  }

  // Monta os cabeçalhos: JSON quando há corpo e o token Bearer quando existe.
  static Map<String, String> _cabecalho({bool json = false, String? bearer}) {
    // Usa o token passado na chamada ou, se não houver, o token da sessão.
    final t = bearer ?? token;
    return {
      'Accept': 'application/json',
      if (json) 'Content-Type': 'application/json; charset=UTF-8',
      if (t != null) 'Authorization': 'Bearer $t',
    };
  }

  // Se a API responder 401 com token ativo, avisa o app para encerrar a sessão.
  static http.Response _verificar(http.Response resposta) {
    if (resposta.statusCode == 401 && token != null) {
      aoNaoAutorizado?.call();
    }
    return resposta;
  }

  // GET com timeout de 5 segundos.
  static Future<http.Response> get(String endpoint) async {
    final resposta = await http
        .get(
      _url(endpoint),
      headers: _cabecalho(),
    )
        .timeout(const Duration(seconds: 5));
    return _verificar(resposta);
  }

  // POST com corpo JSON e timeout de 5 segundos.
  static Future<http.Response> post(
      String endpoint,
      Map<String, dynamic> dados, {
        String? bearer,
      }) async {
    final resposta = await http
        .post(
      _url(endpoint),
      headers: _cabecalho(json: true, bearer: bearer),
      body: jsonEncode(dados),
    )
        .timeout(const Duration(seconds: 5));
    return _verificar(resposta);
  }

  // PUT com corpo JSON e timeout de 5 segundos.
  static Future<http.Response> put(
      String endpoint,
      Map<String, dynamic> dados,
      ) async {
    final resposta = await http
        .put(
      _url(endpoint),
      headers: _cabecalho(json: true),
      body: jsonEncode(dados),
    )
        .timeout(const Duration(seconds: 5));
    return _verificar(resposta);
  }

  // DELETE com timeout de 5 segundos.
  static Future<http.Response> delete(String endpoint) async {
    final resposta = await http
        .delete(
      _url(endpoint),
      headers: _cabecalho(),
    )
        .timeout(const Duration(seconds: 5));
    return _verificar(resposta);
  }

  // GET que espera uma lista JSON; lança exceção se não for 200 ou não for lista.
  static Future<List<Map<String, dynamic>>> getLista(
      String endpoint,
      ) async {
    final resposta = await get(endpoint);

    // Qualquer status diferente de 200 vira erro com o corpo da resposta.
    if (resposta.statusCode != 200) {
      throw Exception(
        'Erro HTTP ${resposta.statusCode}: ${utf8.decode(resposta.bodyBytes)}',
      );
    }

    // Decodifica em UTF-8 para não quebrar acentos.
    final dados = jsonDecode(utf8.decode(resposta.bodyBytes));

    // A API precisa devolver uma lista, não um objeto.
    if (dados is! List) {
      throw const FormatException(
        'A API não retornou uma lista JSON.',
      );
    }

    // Converte cada item em Map<String, dynamic>.
    return dados
        .map((item) => Map<String, dynamic>.from(item as Map))
        .toList();
  }

  // Busca um campo no JSON por vários nomes possíveis, ignorando maiúsculas e símbolos.
  static dynamic valor(
      Map<String, dynamic> dados,
      List<String> nomes,
      ) {
    // Deixa só letras e números em minúsculo para comparar.
    String normalizar(String texto) =>
        texto.toLowerCase().replaceAll(RegExp(r'[^a-z0-9]'), '');

    // Testa os nomes em ordem e devolve o primeiro campo que bater.
    for (final nome in nomes) {      for (final entrada in dados.entries) {
      if (normalizar(entrada.key) == normalizar(nome)) {
        return entrada.value;
      }
    }
    }

    // Não achou nenhum campo.
    return null;
  }
}