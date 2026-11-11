import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../data/repositories/Produto_Repository.dart';
import '../../../data/repositories/Venda_Repository.dart';
import '../../data/api/itens_service.dart';

// Uma linha do extrato: um produto que saiu (venda) do estoque.
class Movimentacao {
  // Nome do produto movimentado.
  final String produtoNome;
  // Negativa para venda, positiva para compra.
  final int quantidade;
  // Data da venda.
  final DateTime data;
  // Texto "venda" ou "compra".
  final String tipo;
  // Total geral da venda.
  final double? totalVenda;
  // Total de desconto da venda.
  final double? totalDesconto;

  Movimentacao({
    required this.produtoNome,
    required this.quantidade,
    required this.data,
    required this.tipo,
    this.totalVenda,
    this.totalDesconto,
  });
}

// Tela que lista as últimas saídas do estoque.
class Extrato extends StatelessWidget {
  const Extrato({super.key});

  // Monta a lista de movimentações, ordenada da mais nova para a mais antiga.
  Future<List<Movimentacao>> _montarMovimentacoes(BuildContext context) async {
    // Pega os repositórios criados no main.dart.
    final produtoRepo = context.read<Produto_Reposity>();
    final vendaRepo = context.read<Venda_Reposity>();
    // Lê os itens de venda direto do banco local.
    final itensVenda = await Itens_Service.itensVenda();

    // Mapa id -> nome do produto, para mostrar o nome em vez do número.
    final Map<int, String> nomes = {
      for (var p in produtoRepo.produtos) p.id!: p.descricao
    };

    final List<Movimentacao> lista = [];

    // Cada item de venda vira uma saída (quantidade negativa).
    for (final venda in vendaRepo.vendas) {
      // Filtra os itens que pertencem a esta venda.
      final itens = itensVenda.where((i) => i.idVenda == venda.id);
      for (final item in itens) {
        lista.add(Movimentacao(
          // Se o produto não está no celular, mostra "Produto <id>".
          produtoNome: nomes[item.idProduto] ?? 'Produto ${item.idProduto}',
          quantidade: -item.qtde,
          data: venda.dataHora,
          tipo: 'venda',
          totalVenda: venda.total,
          totalDesconto: venda.totalDesconto,
        ));
      }
    }

    // Mais recentes primeiro, limitado às 30 últimas.
    lista.sort((a, b) => b.data.compareTo(a.data));
    return lista.take(30).toList();
  }

  @override
  Widget build(BuildContext context) {
    // watch: refaz a tela quando produtos ou vendas mudam.
    context.watch<Produto_Reposity>();
    context.watch<Venda_Reposity>();

    // FutureBuilder espera a lista ser montada para desenhar a tela.
    return FutureBuilder<List<Movimentacao>>(
      future: _montarMovimentacoes(context),
      builder: (context, snapshot) {
        // Enquanto não há dados, usa lista vazia.
        final movimentacoes = snapshot.data ?? [];

        return Scaffold(
          backgroundColor: const Color(0xFFF8FAFC),
          body: SafeArea(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Título e subtítulo da tela.
                const Padding(
                  padding: EdgeInsets.fromLTRB(20, 20, 20, 8),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Extratos',
                        style: TextStyle(
                          fontSize: 28,
                          fontWeight: FontWeight.bold,
                          color: Color(0xFF1E293B),
                        ),
                      ),
                      SizedBox(height: 4),
                      Text(
                        'saídas do estoque.',
                        style: TextStyle(color: Colors.grey, fontSize: 14),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 12),
                Expanded(
                  // Três estados: carregando, vazio ou lista de movimentações.
                  child: snapshot.connectionState == ConnectionState.waiting &&
                      !snapshot.hasData
                      ? const Center(child: CircularProgressIndicator())
                      : movimentacoes.isEmpty
                      ? const Center(
                      child: Text('Nenhuma movimentação ainda'))
                      : ListView.builder(
                    padding:
                    const EdgeInsets.symmetric(horizontal: 16),
                    itemCount: movimentacoes.length,
                    itemBuilder: (context, index) {
                      final m = movimentacoes[index];
                      // Quantidade positiva = entrada; negativa = saída.
                      final isEntrada = m.quantidade > 0;
                      // Cartão branco de cada movimentação.
                      return Container(
                        margin: const EdgeInsets.only(bottom: 10),
                        padding: const EdgeInsets.symmetric(
                            horizontal: 16, vertical: 14),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(12),
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black.withOpacity(0.03),
                              blurRadius: 8,
                            ),
                          ],
                        ),
                        child: Row(
                          children: [
                            // Ícone: seta para cima verde (entrada) ou para baixo vermelha (saída).
                            Container(
                              width: 40,
                              height: 40,
                              decoration: BoxDecoration(
                                color: isEntrada
                                    ? const Color(0xFFDCFCE7)
                                    : const Color(0xFFFEE2E2),
                                borderRadius:
                                BorderRadius.circular(10),
                              ),
                              child: Icon(
                                isEntrada
                                    ? Icons.arrow_upward_rounded
                                    : Icons.arrow_downward_rounded,
                                color: isEntrada
                                    ? const Color(0xFF16A34A)
                                    : const Color(0xFFDC2626),
                                size: 20,
                              ),
                            ),
                            const SizedBox(width: 14),
                            // Nome do produto e data no formato dd/mm/aaaa.
                            Expanded(
                              child: Column(
                                crossAxisAlignment:
                                CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    m.produtoNome,
                                    style: const TextStyle(
                                      fontWeight: FontWeight.w600,
                                      fontSize: 15,
                                    ),
                                  ),
                                  const SizedBox(height: 2),
                                  Text(
                                    '${m.data.day.toString().padLeft(2, '0')}/'
                                        '${m.data.month.toString().padLeft(2, '0')}/'
                                        '${m.data.year}',
                                    style: const TextStyle(
                                      fontSize: 12,
                                      color: Colors.grey,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                            // Quantidade, total da venda e desconto da venda.
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.end,
                              children: [
                                Text(
                                  '${isEntrada ? '+' : ''}${m.quantidade}',
                                  style: TextStyle(
                                    fontWeight: FontWeight.bold,
                                    fontSize: 16,
                                    color: isEntrada
                                        ? const Color(0xFF16A34A)
                                        : const Color(0xFFDC2626),
                                  ),
                                ),
                                if (m.totalVenda != null)
                                  Text(
                                    'Total: R\$ ${m.totalVenda!.toStringAsFixed(2).replaceAll('.', ',')}',
                                    style: const TextStyle(
                                        fontSize: 12, color: Colors.grey),
                                  ),
                                if (m.totalDesconto != null)
                                  Text(
                                    'Desc.: R\$ ${m.totalDesconto!.toStringAsFixed(2).replaceAll('.', ',')}',
                                    style: const TextStyle(
                                        fontSize: 12,
                                        color: Color(0xFFDC2626)),
                                  ),
                              ],
                            ),
                          ],
                        ),
                      );
                    },
                  ),
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}