using System;
using System.Threading.Tasks;
using CardOverflow.Entity;
using Microsoft.EntityFrameworkCore;

namespace CardOverflow.Server {
  public class DbExecutor {
    private readonly DbContextOptions<CardOverflowDb> _options;

    public DbExecutor(DbContextOptions<CardOverflowDb> options) =>
      _options = options;

    public void Command(Action<CardOverflowDb> command) {
      using var db = new CardOverflowDb(_options);
      command(db);
    }

    public T Query<T>(Func<CardOverflowDb, T> query) {
      using var db = new CardOverflowDb(_options);
      return query(db);
    }

    public async Task<T> QueryAsync<T>(Func<CardOverflowDb, Task<T>> query) {
      using var db = new CardOverflowDb(_options);
      return await query(db);
    }

  }
}
