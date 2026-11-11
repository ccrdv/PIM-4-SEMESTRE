import 'package:flutter/material.dart';
import 'package:pim/data/api/api.dart';
import 'package:pim/data/db/db.dart';
import 'package:pim/ui/telas/Cadastro.screen.dart';
import 'package:pim/ui/telas/Extrato.screen.dart';
import 'package:pim/ui/telas/Geral.screen.dart';
import 'package:provider/provider.dart';
import 'package:pim/ui/telas/Login.screen.dart';
import 'package:pim/data/repositories/Categoria_Repository.dart';
import 'package:pim/data/repositories/Produto_Repository.dart';
import 'package:pim/data/repositories/Venda_Repository.dart';
import 'core/navegacao.dart';
import 'core/rotas.dart';
import 'core/tema.dart';

import 'data/api/sessao.dart';
import 'homepage.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await Db.instance.database;
  ApiConnection.aoNaoAutorizado = encerrarSessao;
  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (context) => Categoria_Reposity()),
        ChangeNotifierProvider(create: (context) => Produto_Reposity()),
        ChangeNotifierProvider(create: (context) => Venda_Reposity()),
      ],
      child: const PIM(),
    ),
  );
}

class PIM extends StatelessWidget {
  const PIM({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'PIM',
      debugShowCheckedModeBanner: false,
      theme: TemaApp.tema,
      navigatorKey: navigatorKey,
      onGenerateInitialRoutes: (_) => [
        MaterialPageRoute(
          builder: (_) => Sessao.existe ? const HomePage() : const Login(),
        ),
      ],
      routes: {
        Rotas.login: (context) => const Login(),
        Rotas.home: (context) => const HomePage(),
        Rotas.cadastro: (context) => const Cadastro(),
        Rotas.extrato: (context) => const Extrato(),
        Rotas.geral: (context) => const Geral(),
      },
    );
  }
}