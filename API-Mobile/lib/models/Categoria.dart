class Categoria {
  final int? id;
  final String descricao;

  Categoria({this.id, required this.descricao});

  Map<String, dynamic> toMap() => {
    'Id_Categoria': id,
    'Descricao_Categoria': descricao,
  };

  factory Categoria.fromMap(Map<String, dynamic> map) => Categoria(
    id: map['Id_Categoria'] as int?,
    descricao: map['Descricao_Categoria'] as String,
  );
}