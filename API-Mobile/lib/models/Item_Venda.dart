class ItemVenda {
  final int? id;
  final int qtde;
  final double precoUnitario;
  final double valorDesconto;
  final double valorTotal;
  final int idVenda;
  final int idProduto;

  ItemVenda({
    this.id,
    required this.qtde,
    required this.precoUnitario,
    required this.valorDesconto,
    required this.valorTotal,
    required this.idVenda,
    required this.idProduto,
  });

  Map<String, dynamic> toMap() => {
    'Id_Item_Venda': id,
    'QDTE_Item_Venda': qtde,
    'Preco_Unitario_Item_Venda': precoUnitario,
    'Valor_Desconto_Item_Venda': valorDesconto,
    'Valor_Total_Item_Venda': valorTotal,
    'Id_Venda': idVenda,
    'Id_Produto': idProduto,
  };

  factory ItemVenda.fromMap(Map<String, dynamic> map) => ItemVenda(
    id: map['Id_Item_Venda'] as int?,
    qtde: map['QDTE_Item_Venda'] as int,
    precoUnitario: (map['Preco_Unitario_Item_Venda'] as num).toDouble(),
    valorDesconto: (map['Valor_Desconto_Item_Venda'] as num).toDouble(),
    valorTotal: (map['Valor_Total_Item_Venda'] as num).toDouble(),
    idVenda: map['Id_Venda'] as int,
    idProduto: map['Id_Produto'] as int,
  );
}