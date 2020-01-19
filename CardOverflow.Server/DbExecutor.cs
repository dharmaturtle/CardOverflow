using System;
using System.Threading.Tasks;
using CardOverflow.Entity;
using Microsoft.EntityFrameworkCore;

namespace CardOverflow.Server {
  public class DbExecutor {
    private readonly DbContextOptions<CardOverflowDb> _options;
    private readonly IEntityHasher _entityHasher;

    public DbExecutor(DbContextOptions<CardOverflowDb> options, IEntityHasher entityHasher) {
      _options = options;
      _entityHasher = entityHasher;
    }

    public void Command(Action<CardOverflowDb> command) {
      using var db = new CardOverflowDb(_options, _entityHasher);
      command(db);
    }

    public T Query<T>(Func<CardOverflowDb, T> query) {
      using var db = new CardOverflowDb(_options, _entityHasher);
      return query(db);
    }

    public async Task<T> QueryAsync<T>(Func<CardOverflowDb, Task<T>> query) {
      using var db = new CardOverflowDb(_options, _entityHasher);
      return await query(db);
    }

  }
}
