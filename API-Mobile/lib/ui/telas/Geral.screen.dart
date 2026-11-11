import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../models/Produto.dart';
import '../../../models/Item_Venda.dart';
import '../../../data/repositories/Produto_Repository.dart';
import '../../../data/repositories/Venda_Repository.dart';
import '../../data/api/itens_service.dart';

// Tela inicial com o resumo do estoque e o gráfico de vendas da semana.
class Geral extends StatelessWidget {
  const Geral({super.key});

  @override
  Widget build(BuildContext context) {
    // watch: a tela se refaz quando produtos ou vendas mudam.
    final produtoRepo = context.watch<Produto_Reposity>();
    final vendaRepo = context.watch<Venda_Reposity>();

    // Lê os itens de venda do banco local e desenha quando chegarem.
    return FutureBuilder<List<ItemVenda>>(
      future: Itens_Service.itensVenda(),
      builder: (context, snapshot) {
        // Enquanto carrega, usa lista vazia.
        final todosItens = snapshot.data ?? [];

        // Soma a quantidade de todos os produtos para o card de estoque.
        final produtos = produtoRepo.produtos;
        final totalEstoque = produtos.fold<int>(0, (sum, p) => sum + p.qtde);

        // Janela da semana: de 7 dias atrás até agora.
        final agora = DateTime.now();
        final inicioSemana = agora.subtract(const Duration(days: 7));

        // Só as vendas dos últimos 7 dias.
        final vendasSemana = vendaRepo.vendas
            .where((v) => v.dataHora.isAfter(inicioSemana))
            .toList();

        // Total vendido e total por dia da semana (0 = domingo ... 6 = sábado).
        int unidadesVendidas = 0;
        final Map<int, int> vendasPorDia = {for (var i = 0; i < 7; i++) i: 0};

        for (final venda in vendasSemana) {
          // Soma a quantidade dos itens que pertencem a esta venda.
          final itens = todosItens.where((i) => i.idVenda == venda.id);
          final totalQtde = itens.fold<int>(0, (s, i) => s + i.qtde);
          unidadesVendidas += totalQtde;

          // weekday vai de 1 (seg) a 7 (dom); o % 7 faz domingo virar 0.
          final dia = venda.dataHora.weekday % 7;
          vendasPorDia[dia] = (vendasPorDia[dia] ?? 0) + totalQtde;
        }
        // Vendas de hoje: bruto, desconto e líquido.
        final vendasHoje = vendaRepo.vendas.where((v) =>
        v.dataHora.year == agora.year &&
            v.dataHora.month == agora.month &&
            v.dataHora.day == agora.day);

        final brutoHoje = vendasHoje.fold<double>(0, (s, v) => s + v.total);
        final descontoHoje =
        vendasHoje.fold<double>(0, (s, v) => s + v.totalDesconto);
        final liquidoHoje = brutoHoje - descontoHoje;

        String moeda(double v) => 'R\$ ${v.toStringAsFixed(2).replaceAll('.', ',')}';
        return Scaffold(
          backgroundColor: const Color(0xFFF8FAFC),
          body: SafeArea(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Título e subtítulo da tela.
                  const Text(
                    'Geral',
                    style: TextStyle(
                      fontSize: 28,
                      fontWeight: FontWeight.bold,
                      color: Color(0xFF1E293B),
                    ),
                  ),
                  const SizedBox(height: 4),
                  const Text(
                    'Geral do estoque.',
                    style: TextStyle(color: Colors.grey, fontSize: 14),
                  ),
                  const SizedBox(height: 24),
                  // Dois cards lado a lado: vendas da semana e estoque atual.
                  Row(
                    children: [
                      Expanded(
                        child: _ResumoCard(
                          titulo: 'Vendas na semana',
                          valor: '$unidadesVendidas',
                          unidade: 'unidades',
                          cor: const Color(0xFF3B82F6),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: _ResumoCard(
                          titulo: 'Estoque atual',
                          valor: '$totalEstoque',
                          unidade: 'unidades',
                          cor: const Color(0xFF10B981),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 28),
                  const Text(
                    'Vendas da semana',
                    style: TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w600,
                      color: Color(0xFF1E293B),
                    ),
                  ),
                  const SizedBox(height: 12),
                  // Caixa do gráfico de barras, desenhado à mão com Containers.
                  Container(
                    height: 170,
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(16),
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withOpacity(0.04),
                          blurRadius: 10,
                        ),
                      ],
                    ),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      mainAxisAlignment: MainAxisAlignment.spaceAround,
                      // Uma coluna por dia da semana (7 barras).
                      children: List.generate(7, (i) {
                        // Maior valor do gráfico, usado como 100% da altura.
                        final max =
                        vendasPorDia.values.fold(0, (a, b) => a > b ? a : b);
                        final valor = vendasPorDia[i] ?? 0;
                        // Altura proporcional ao maior valor, até 80 pixels.
                        final altura = max == 0 ? 0.0 : (valor / max) * 80;
                        final dias = [
                          'Dom',
                          'Seg',
                          'Ter',
                          'Qua',
                          'Qui',
                          'Sex',
                          'Sáb'
                        ];

                        return Column(
                          mainAxisAlignment: MainAxisAlignment.end,
                          children: [
                            // Número de unidades acima da barra.
                            Text(
                              valor.toString(),
                              style: const TextStyle(
                                  fontSize: 10, color: Colors.grey),
                            ),
                            const SizedBox(height: 4),
                            // A barra: mínimo de 4 pixels para dias sem venda ficarem visíveis.
                            Container(
                              width: 28,
                              height: altura.clamp(4, 80),
                              decoration: BoxDecoration(
                                color: const Color(0xFF3B82F6),
                                borderRadius: BorderRadius.circular(6),
                              ),
                            ),
                            const SizedBox(height: 6),
                            // Nome do dia embaixo da barra.
                            Text(
                              dias[i],
                              style: const TextStyle(
                                  fontSize: 11, color: Colors.grey),
                            ),
                          ],
                        );
                      }),
                    ),
                  ),
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      Expanded(
                        child: _ResumoCard(
                          titulo: 'Total hoje geral',
                          valor: moeda(brutoHoje),
                          unidade: '',
                          cor: const Color(0xFF8B5CF6),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: _ResumoCard(
                          titulo: 'total hoje desconto',
                          valor: moeda(liquidoHoje),
                          unidade: '',
                          cor: const Color(0xFFF59E0B),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 28),
                  const Text(
                    'Estoque por produto',
                    style: TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w600,
                      color: Color(0xFF1E293B),
                    ),
                  ),
                  const SizedBox(height: 12),
                  // Lista de produtos com a quantidade, ou aviso se não há nenhum.
                  if (produtos.isEmpty)
                    const Center(
                      child: Padding(
                        padding: EdgeInsets.all(32),
                        child: Text('Nenhum produto cadastrado'),
                      ),
                    )
                  else
                    ...produtos.map((p) => _ProdutoEstoqueTile(produto: p)),
                ],
              ),
            ),
          ),
        );
      },
    );
  }
}

// Cartão pequeno com título, número grande colorido e unidade.
class _ResumoCard extends StatelessWidget {
  final String titulo;
  final String valor;
  final String unidade;
  final Color cor;

  const _ResumoCard({
    required this.titulo,
    required this.valor,
    required this.unidade,
    required this.cor,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(color: Colors.black.withOpacity(0.04), blurRadius: 10),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Título pequeno em cinza.
          Text(titulo,
              style: const TextStyle(fontSize: 12, color: Colors.grey)),
          const SizedBox(height: 8),
          Row(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              // Número grande na cor do card.
              Text(
                valor,
                style: TextStyle(
                  fontSize: 28,
                  fontWeight: FontWeight.bold,
                  color: cor,
                ),
              ),
              const SizedBox(width: 4),
              // Unidade ao lado do número.
              Padding(
                padding: const EdgeInsets.only(bottom: 4),
                child: Text(unidade,
                    style: const TextStyle(fontSize: 12, color: Colors.grey)),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

// Linha da lista de estoque: nome do produto à esquerda e quantidade à direita.
class _ProdutoEstoqueTile extends StatelessWidget {
  final Produto produto;

  const _ProdutoEstoqueTile({required this.produto});

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        boxShadow: [
          BoxShadow(color: Colors.black.withOpacity(0.03), blurRadius: 8),
        ],
      ),
      child: Row(
        children: [
          // Nome ocupa o espaço disponível.
          Expanded(
            child: Text(
              produto.descricao,
              style: const TextStyle(fontWeight: FontWeight.w500),
            ),
          ),
          // Quantidade em estoque em negrito.
          Text(
            '${produto.qtde}',
            style: const TextStyle(
              fontWeight: FontWeight.bold,
              fontSize: 16,
              color: Color(0xFF1E293B),
            ),
          ),
        ],
      ),
    );
  }
}