import 'package:flutter/material.dart';
import '../data/api/sessao.dart';
import 'rotas.dart';

// Chave global que permite navegar de qualquer lugar, sem precisar de context.
final GlobalKey<NavigatorState> navigatorKey = GlobalKey<NavigatorState>();

// Trava para não encerrar a sessão duas vezes ao mesmo tempo.
bool _encerrando = false;

// Chamada quando a API responde 401: apaga a sessão e volta para o login.
Future<void> encerrarSessao() async {
  // Se já está encerrando, ignora a chamada repetida.
  if (_encerrando) return;
  _encerrando = true;

  try {
    // Apaga API, token e validade do aparelho.
    await Sessao.limpar();

    // Pega o navegador; se ainda não existe, não há para onde ir.
    final navegador = navigatorKey.currentState;
    if (navegador == null) return;

    // Vai para o login e remove todas as telas anteriores da pilha.
    navegador.pushNamedAndRemoveUntil(Rotas.login, (_) => false);

    // Espera a tela de login desenhar para poder mostrar o aviso.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final contexto = navigatorKey.currentContext;
      if (contexto == null) return;
      // Avisa o usuário de que precisa ler o QR code de novo.
      ScaffoldMessenger.of(contexto).showSnackBar(
        const SnackBar(
          content: Text('Sessão encerrada. Leia o QR code novamente.'),
        ),
      );
    });
  } finally {
    // Libera a trava, mesmo se algo der erro.
    _encerrando = false;
  }
}