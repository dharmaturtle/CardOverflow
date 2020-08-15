using System;
using System.Threading.Tasks;
using CardOverflow.Entity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CardOverflow.Server {
  public class DbExecutor {
    private readonly DbContextOptions<CardOverflowDb> _options;
    private readonly Func<Task<NpgsqlConnection>> _npgsqlConnectionFunc;

    public DbExecutor(DbContextOptions<CardOverflowDb> options, Func<Task<NpgsqlConnection>> npgsqlConnectionFunc) {
      _options = options;
      _npgsqlConnectionFunc = npgsqlConnectionFunc;
    }

    public void Command(Action<CardOverflowDb> command) {
      using var db = new CardOverflowDb(_options);
      command(db);
    }

    public async Task CommandAsync(Func<CardOverflowDb, Task> command) {
      using var db = new CardOverflowDb(_options);
      await command(db);
    }

    public T Query<T>(Func<CardOverflowDb, T> query) {
      using var db = new CardOverflowDb(_options);
      return query(db);
    }

    public async Task<T> QueryAsync<T>(Func<CardOverflowDb, Task<T>> query) {
      using var db = new CardOverflowDb(_options);
      return await query(db);
    }

    public async Task<T> QueryAsync<T>(Func<NpgsqlConnection, Task<T>> query) {
      var conn = await _npgsqlConnectionFunc.Invoke();
      return await query(conn);
    }

  }
}
