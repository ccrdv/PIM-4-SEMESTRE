import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:pim/data/repositories/Categoria_Repository.dart';
import 'package:pim/data/repositories/Produto_Repository.dart';
import 'package:pim/data/repositories/Venda_Repository.dart';

import 'package:pim/ui/telas/Cadastro.screen.dart';
import 'package:pim/ui/telas/Extrato.screen.dart';
import 'package:pim/ui/telas/Geral.screen.dart';

import 'data/api/auth_service.dart';
import 'data/api/sessao.dart';

// Tela principal: barra de cima, as 3 abas no meio e a navegação embaixo.
class HomePage extends StatefulWidget {
  const HomePage({super.key});

  @override
  State<HomePage> createState() => _HomePageState();
}

// WidgetsBindingObserver permite saber quando o app vai para segundo plano.
class _HomePageState extends State<HomePage> with WidgetsBindingObserver {
  // Índice da aba atual (0 = Geral, 1 = Extratos, 2 = Cadastrar).
  int paginaAtual = 0;
  // Controla a troca de páginas deslizando ou tocando na barra.
  late PageController pc;
  // Trava o botão de atualizar enquanto uma atualização está rodando.
  bool _atualizando = false;

  @override
  void initState() {
    super.initState();
    pc = PageController(initialPage: paginaAtual);
    // Começa a observar o ciclo de vida do app.
    WidgetsBinding.instance.addObserver(this);
    // Depois de desenhar a primeira vez, executa a rotina de abertura.
    WidgetsBinding.instance.addPostFrameCallback((_) => _aoAbrir());
  }

  @override
  void dispose() {
    // Libera o observador e o controlador ao sair da tela.
    WidgetsBinding.instance.removeObserver(this);
    pc.dispose();
    super.dispose();
  }

  // Ao abrir: valida a sessão e, se existir, envia os produtos pendentes.
  Future<void> _aoAbrir() async {
    await Auth_Service.validarSessao();
    // Sem tela ou sem sessão, não há o que enviar.
    if (!mounted || !Sessao.existe) return;
    await context.read<Produto_Reposity>().enviarPendentes();
  }

  // Quando o app vai para segundo plano, tenta enviar as pendências.
  @override
  void didChangeAppLifecycleState(AppLifecycleState estado) {
    if (estado == AppLifecycleState.paused && Sessao.existe) {
      context.read<Produto_Reposity>().enviarPendentes();
    }
    if (estado == AppLifecycleState.resumed && Sessao.existe) {
      Auth_Service.validarSessao();
    }
  }

  // Guarda a aba atual e redesenha a barra de navegação.
  void setPaginaAtual(int pagina) {
    setState(() {
      paginaAtual = pagina;
    });
  }

  // Botão de atualizar: valida o token e, se estiver válido, atualiza os dados em silêncio.
  Future<void> _atualizarDados() async {
    if (_atualizando) return;
    setState(() => _atualizando = true);

    // Pega os repositórios antes dos await.
    final produtos = context.read<Produto_Reposity>();
    final categorias = context.read<Categoria_Reposity>();
    final vendas = context.read<Venda_Reposity>();

    try {
      // true = válido, false = 401 (inválido), null = offline/servidor fora.
      final sessao = await Auth_Service.validarSessao();
      if (!mounted) return;

      final String texto;
      final Color cor;

      if (sessao == true) {
        texto = 'Token válido';
        cor = Colors.green;
      } else if (sessao == false) {
        // O 401 já aciona o encerrarSessao e leva ao login.
        texto = 'Token inválido, deslogando...';
        cor = Colors.red;
      } else {
        texto = 'Offline';
        cor = Colors.orange;
      }

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(texto),
          backgroundColor: cor,
          duration: const Duration(seconds: 3),
        ),
      );

      // Só atualiza os dados se o token está válido (em silêncio, erros só no log).
      if (sessao == true) {
        try {
          await produtos.enviarPendentes();
          await categorias.atualizarDaApi();
          await produtos.atualizarDaApi();
          await vendas.atualizarDaApi();
        } catch (e) {
          debugPrint('Erro ao atualizar dados: $e');
        }
      }
    } finally {
      if (mounted) setState(() => _atualizando = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      // Barra de cima com o título e o botão de atualizar.
      appBar: AppBar(
        title: const Text('Sistema White Label'),
        actions: [
          IconButton(
            tooltip: 'Atualizar dados',
            // Desativado enquanto atualiza.
            onPressed: _atualizando ? null : _atualizarDados,
            // Mostra um círculo girando durante a atualização.
            icon: _atualizando
                ? const SizedBox(
              width: 22,
              height: 22,
              child: CircularProgressIndicator(strokeWidth: 2),
            )
                : const Icon(Icons.refresh),
          ),
        ],
      ),
      // Corpo: as 3 telas lado a lado, com deslize entre elas.
      body: PageView(
        controller: pc,
        onPageChanged: setPaginaAtual,
        children: const [
          Geral(),
          Extrato(),
          Cadastro(),
        ],
      ),
      // Barra de baixo com os 3 botões das abas.
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: paginaAtual,
        type: BottomNavigationBarType.fixed,
        items: const [
          BottomNavigationBarItem(
            icon: Icon(Icons.home),
            label: 'Geral',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.swap_vert),
            label: 'Extratos',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.add_box),
            label: 'Cadastrar',
          ),
        ],
        // Ao tocar num item, desliza até a página correspondente.
        onTap: (pagina) {
          pc.animateToPage(
            pagina,
            duration: const Duration(milliseconds: 400),
            curve: Curves.ease,
          );
        },
      ),
    );
  }
}