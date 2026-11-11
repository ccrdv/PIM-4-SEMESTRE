class Venda {
  final int? id;
  final DateTime dataHora;
  final double total;
  final double totalDesconto;
  final String? status;
  final int? idUsuario;

  Venda({
    this.id,
    required this.dataHora,
    required this.total,
    required this.totalDesconto,
    this.status,
    this.idUsuario,
  });

  Map<String, dynamic> toMap() => {
    'Id_Venda': id,
    'Data_Hora_Venda': dataHora.toIso8601String(),
    'Total_Venda': total,
    'Total_Desconto_Venda': totalDesconto,
    'Status_Venda_Venda': status,
    'Id_Usuario': idUsuario,
  };

  factory Venda.fromMap(Map<String, dynamic> map) => Venda(
    id: map['Id_Venda'] as int?,
    dataHora: DateTime.parse(map['Data_Hora_Venda'] as String),
    total: (map['Total_Venda'] as num).toDouble(),
    totalDesconto: (map['Total_Desconto_Venda'] as num).toDouble(),
    status: map['Status_Venda_Venda'] as String?,
    idUsuario: map['Id_Usuario'] as int?,
  );
}