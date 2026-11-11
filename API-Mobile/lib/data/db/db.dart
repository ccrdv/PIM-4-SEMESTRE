import 'package:path/path.dart';
import 'package:sqflite/sqflite.dart';

class Db {
  Db._();

  static final Db instance = Db._();
  static Database? _database;

  Future<Database> get database async {
    if (_database != null && _database!.isOpen) {
      return _database!;
    }

    _database = await _initDatabase();
    return _database!;
  }

  Future<Database> _initDatabase() async {
    final caminho = join(
      await getDatabasesPath(),
      'PIM_7.0.db',
    );

    return openDatabase(
      caminho,
      version: 2,
      onCreate: _onCreate,
      onUpgrade: _onUpgrade,
    );
  }

  Future<void> _onUpgrade(Database db, int antiga, int nova) async {
    if (antiga < 2) {
      await db.execute('ALTER TABLE tbl_Produto ADD COLUMN Cliente_Id TEXT');
    }
  }

  Future<void> _onCreate(Database db, int versao) async {
    await db.execute('''
      CREATE TABLE tbl_Categoria (
        Id_Categoria INTEGER PRIMARY KEY AUTOINCREMENT,
        Descricao_Categoria TEXT NOT NULL
      )
    ''');

    await db.execute('''
      CREATE TABLE tbl_Produto (
        Id_Produto INTEGER PRIMARY KEY AUTOINCREMENT,
        Descricao_Produto TEXT NOT NULL,
        Preco_Compra_Produto REAL NOT NULL,
        preco_Venda_Produto REAL NOT NULL,
        QDTE_Produto INTEGER NOT NULL,
        Id_Categoria INTEGER NOT NULL,
        Pendente INTEGER NOT NULL DEFAULT 0,
        Erro TEXT,
        FOREIGN KEY (Id_Categoria)
          REFERENCES tbl_Categoria (Id_Categoria)
      )
    ''');


    await db.execute('''
      CREATE TABLE tbl_Venda (
        Id_Venda INTEGER PRIMARY KEY AUTOINCREMENT,
        Data_Hora_Venda TEXT NOT NULL,
        Total_Venda REAL NOT NULL,
        Total_Desconto_Venda REAL NOT NULL,
        Status_Venda_Venda TEXT,
        Id_Usuario INTEGER
      )
    ''');

    await db.execute('''
      CREATE TABLE tbl_Item_Venda (
        Id_Item_Venda INTEGER PRIMARY KEY AUTOINCREMENT,
        QDTE_Item_Venda INTEGER NOT NULL,
        Preco_Unitario_Item_Venda REAL NOT NULL,
        Valor_Desconto_Item_Venda REAL NOT NULL,
        Valor_Total_Item_Venda REAL NOT NULL,
        Id_Venda INTEGER NOT NULL,
        Id_Produto INTEGER NOT NULL,
        FOREIGN KEY (Id_Venda)
          REFERENCES tbl_Venda (Id_Venda),
        FOREIGN KEY (Id_Produto)
          REFERENCES tbl_Produto (Id_Produto)
      )
    ''');
  }
}