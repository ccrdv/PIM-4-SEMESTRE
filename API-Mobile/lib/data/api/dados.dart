import 'package:sqflite/sqflite.dart';
import 'api.dart';

class Dados {
  // Baixa a lista da API e espelha no banco local; opcionalmente apaga o que sumiu do servidor.
  static Future<void> sincronizar(
      Database db,
      String tabela,
      String endpoint,
      Map<String, List<String>> chaves, {
        // Se true, apaga do celular os registros que não vieram da API.
        bool removerAusentes = false,
        // Se true, protege os registros com Pendente = 1 (ainda não enviados).
        bool temPendente = false,
      }) async {
    // Baixa primeiro; se falhar, lança exceção e nada é alterado no banco.
    final dados = await ApiConnection.getLista(endpoint);
    // A primeira chave do mapa é a chave primária da tabela.
    final pk = chaves.keys.first;
    // Guarda os ids que vieram do servidor, para a limpeza no final.
    final idsServidor = <Object?>{};

    // Tudo numa transação: ou grava tudo, ou nada.
    await db.transaction((txn) async {
      for (final item in dados) {
        // Registro local montado a partir do JSON.
        final reg = <String, dynamic>{};

        // Para cada coluna local, procura o valor no JSON pelos nomes possíveis.
        chaves.forEach((destino, origens) {
          final valor = ApiConnection.valor(item, [destino, ...origens]);
          if (valor != null) {
            // SQLite não tem bool: converte para 1 ou 0.
            reg[destino] = valor is bool ? (valor ? 1 : 0) : valor;
          }
        });

        // Item sem id não dá para gravar: ignora.
        if (reg[pk] == null) continue;
        idsServidor.add(reg[pk]);

        // Tenta atualizar o registro que já existe.
        final alterados = await txn.update(
          tabela,
          reg,
          where: '$pk = ?',
          whereArgs: [reg[pk]],
        );
        // Se nenhum registro foi alterado, ele é novo: insere.
        if (alterados == 0) {
          await txn.insert(tabela, reg);
        }
      }

      if (removerAusentes) {
        // Lê os ids locais, deixando de fora os pendentes quando pedido.
        final locais = await txn.query(
          tabela,
          columns: [pk],
          where: temPendente ? 'Pendente = 0' : null,
        );
        // Apaga o que existe no celular mas não veio do servidor.
        for (final l in locais) {
          if (!idsServidor.contains(l[pk])) {
            await txn.delete(tabela, where: '$pk = ?', whereArgs: [l[pk]]);
          }
        }
      }
    });
  }
}