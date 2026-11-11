class Produto {
  final int? id;
  final String descricao;
  final double precoCompra;
  final double precoVenda;
  final int qtde;
  final int idCategoria;

  Produto({
    this.id,
    required this.descricao,
    required this.precoCompra,
    required this.precoVenda,
    required this.qtde,
    required this.idCategoria,
  });

  Map<String, dynamic> toMap() => {
    'Id_Produto': id,
    'Descricao_Produto': descricao,
    'Preco_Compra_Produto': precoCompra,
    'preco_Venda_Produto': precoVenda,
    'QDTE_Produto': qtde,
    'Id_Categoria': idCategoria,
  };

  factory Produto.fromMap(Map<String, dynamic> map) => Produto(
    id: map['Id_Produto'] as int?,
    descricao: map['Descricao_Produto'] as String,
    precoCompra: (map['Preco_Compra_Produto'] as num).toDouble(),
    precoVenda: (map['preco_Venda_Produto'] as num).toDouble(),
    qtde: map['QDTE_Produto'] as int,
    idCategoria: map['Id_Categoria'] as int,
  );
}