# App Flutter: como entrar na API

O celular **não tem tela de login**. Quem autoriza é o PC (que já passou por usuário, senha e 2FA):

1. No PC: **Configurações → Celulares conectados → Conectar celular**. Aparece um QR Code (vale 2 minutos, uso único).
2. No celular: o app lê o QR Code e chama `POST /api/Dispositivo/parear`. Recebe um token e guarda.
3. O token vale **3 dias** (`Dispositivos:DiasDeSessao` no `appsettings.json`). Antes de vencer, o PC pode tocar em **Renovar**
   (mais 3 dias, até o limite de 30 dias contados do pareamento). Vencido, ou depois de **Desconectar** no PC, a API responde
   **401** e o app volta para a tela de ler o QR Code.

O celular usa o **mesmo perfil** de quem autorizou (Administrador, Gerente ou Vendedor) e as mesmas rotas do sistema web,
com o header `Authorization: Bearer <token>`. Ele não consegue autorizar outro aparelho nem mexer na lista de celulares.

## Rotas

| Quem | Rota | Para quê |
|---|---|---|
| PC | `POST /api/Dispositivo/pareamento` | gera o código do QR Code |
| Celular | `POST /api/Dispositivo/parear` `{ "codigo": "...", "nomeDispositivo": "Pixel 7" }` | troca o código por token (sem login) |
| Celular | `GET /api/Dispositivo/minha-sessao` | até quando a sessão vale |
| PC | `GET /api/Dispositivo` | lista os celulares |
| PC | `POST /api/Dispositivo/{id}/renovar` | mais 3 dias |
| PC | `DELETE /api/Dispositivo/{id}` | desconecta na hora |

O QR Code carrega um JSON: `{"api": "http://IP:porta/api", "codigo": "..."}`. O endereço vem do que está na barra do navegador
do PC, então o sistema precisa ser aberto pelo IP da rede/VPN (não por `localhost`) para o celular alcançar a API.

## Exemplo em Dart (não testado)

Pacotes: `mobile_scanner`, `http`, `flutter_secure_storage`.

```dart
import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

const _storage = FlutterSecureStorage();

class Sessao {
  // Chamado quando o mobile_scanner lê o QR Code
  static Future<void> parear(String conteudoDoQr, String nomeDoAparelho) async {
    final qr = jsonDecode(conteudoDoQr) as Map<String, dynamic>;
    final api = qr['api'] as String;

    final resp = await http.post(
      Uri.parse('$api/Dispositivo/parear'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'codigo': qr['codigo'], 'nomeDispositivo': nomeDoAparelho}),
    );

    if (resp.statusCode != 200) {
      throw Exception(jsonDecode(resp.body)['mensagem'] ?? 'Erro ao conectar');
    }

    final dados = jsonDecode(resp.body);
    await _storage.write(key: 'api', value: api);
    await _storage.write(key: 'token', value: dados['accessToken']);
    await _storage.write(key: 'expiraEm', value: dados['expiraEm']);
  }

  // Todas as chamadas passam por aqui. 401 = sessão vencida ou desconectada: voltar para a tela do QR Code.
  static Future<http.Response> get(String rota) async {
    final api = await _storage.read(key: 'api');
    final token = await _storage.read(key: 'token');

    final resp = await http.get(
      Uri.parse('$api$rota'),
      headers: {'Authorization': 'Bearer $token'},
    );

    if (resp.statusCode == 401) {
      await _storage.deleteAll();
      throw SessaoExpirada();
    }
    return resp;
  }
}

class SessaoExpirada implements Exception {}
```

Para mostrar "sua sessão acaba em X dias", chame `GET /Dispositivo/minha-sessao`.
