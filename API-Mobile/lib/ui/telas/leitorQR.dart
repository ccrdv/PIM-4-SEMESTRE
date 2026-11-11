import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:pim/data/api/auth_service.dart';
import 'package:pim/data/api/sincronizador.dart';
import 'package:pim/homepage.dart';

// Tela que abre a câmera para ler o QR code e parear o aparelho com a API.
class LeitorQr extends StatefulWidget {
  const LeitorQr({super.key});

  @override
  State<LeitorQr> createState() => _LeitorQrState();
}

class _LeitorQrState extends State<LeitorQr> {
  // Controla a câmera: ignora leituras repetidas e só aceita QR code.
  final _controller = MobileScannerController(
    detectionSpeed: DetectionSpeed.noDuplicates,
    formats: [BarcodeFormat.qrCode],
  );
  // Trava para não processar dois QR codes ao mesmo tempo.
  bool _processando = false;
  // Mensagem de erro mostrada na faixa de baixo (nula = sem erro).
  String? _erro;

  // Desliga a câmera ao sair da tela.
  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  // Chamada toda vez que a câmera detecta um QR code.
  Future<void> _aoLer(BarcodeCapture captura) async {
    // Se já está processando, ignora novas leituras.
    if (_processando) return;
    if (captura.barcodes.isEmpty) return;
    // Texto bruto lido do QR code.
    final codigo = captura.barcodes.first.rawValue;
    if (codigo == null || codigo.isEmpty) return;

    // Mostra o indicador de carregamento e limpa o erro anterior.
    setState(() {
      _processando = true;
      _erro = null;
    });

    // Envia o conteúdo do QR para a API e espera o resultado do pareamento.
    final resultado = await Auth_Service.parear(codigo);
    // Se a tela foi fechada durante a espera, não mexe mais nela.
    if (!mounted) return;

    if (resultado == ResultadoLogin.ok) {
      // Pareou: já baixa todos os dados da API.
      await Sincronizador.tudo(context);
      if (!mounted) return;
      // Vai para a HomePage e apaga as telas anteriores da pilha.
      Navigator.pushAndRemoveUntil(
        context,
        MaterialPageRoute(builder: (_) => const HomePage()),
            (route) => false,
      );
      return;
    }

    // Falhou: mostra o motivo e libera para tentar outro QR.
    setState(() {
      _erro = resultado == ResultadoLogin.semConexao
          ? 'Sem conexão com o servidor'
          : 'QR code inválido ou expirado';
      _processando = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Entrar com QR code')),
      // Stack: a câmera ocupa tudo e a faixa de aviso fica por cima.
      body: Stack(
        children: [
          // Visualização da câmera; chama _aoLer ao detectar um QR.
          MobileScanner(controller: _controller, onDetect: _aoLer),
          // Faixa escura na parte de baixo com a mensagem.
          Align(
            alignment: Alignment.bottomCenter,
            child: Container(
              width: double.infinity,
              color: Colors.black54,
              padding: const EdgeInsets.all(20),
              // Carregando: círculo girando; senão: erro em vermelho ou instrução.
              child: _processando
                  ? const Center(
                child: CircularProgressIndicator(color: Colors.white),
              )
                  : Text(
                _erro ?? 'Aponte a câmera para o QR code da web',
                textAlign: TextAlign.center,
                style: TextStyle(
                  color: _erro != null ? Colors.redAccent : Colors.white,
                  fontSize: 16,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}