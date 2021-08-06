using System;
using System.Threading.Tasks;
using CardOverflow.Entity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ThoughtDesign.WebLibrary {
  public class DbExecutor {
    private readonly Func<Task<NpgsqlConnection>> _npgsqlConnectionFunc;

    public DbExecutor(Func<Task<NpgsqlConnection>> npgsqlConnectionFunc) {
      _npgsqlConnectionFunc = npgsqlConnectionFunc;
    }

    public void Command(Action<CardOverflowDb> command) {
      using var db = new CardOverflowDb();
      command(db);
    }

    public async Task CommandAsync(Func<CardOverflowDb, Task> command) {
      using var db = new CardOverflowDb();
      await command(db);
    }

    public T Query<T>(Func<CardOverflowDb, T> query) {
      using var db = new CardOverflowDb();
      return query(db);
    }

    public async Task<T> QueryAsync<T>(Func<CardOverflowDb, Task<T>> query) {
      using var db = new CardOverflowDb();
      return await query(db);
    }

    public async Task<T> QueryAsync<T>(Func<NpgsqlConnection, Task<T>> query) {
      var conn = await _npgsqlConnectionFunc.Invoke();
      return await query(conn);
    }

    public CardOverflowDb Get() => new CardOverflowDb();

  }
}
