using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Microsoft.FSharp.Core;
using System.Collections.Generic;

namespace CardOverflow.Entity {

  public interface IEntityHasher {
    FSharpFunc<(CardInstanceEntity, byte[], SHA512), byte[]> CardInstanceHasher { get; }
    FSharpFunc<(CardTemplateInstanceEntity, SHA512), byte[]> CardTemplateInstanceHasher { get; }
  }

  public partial class CardOverflowDb : IdentityDbContext<UserEntity, IdentityRole<int>, int> {
    private readonly IEntityHasher _entityHasher;

    public CardOverflowDb(DbContextOptions<CardOverflowDb> options, IEntityHasher entityHasher) : base(options) {
      _entityHasher = entityHasher;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess) {
      _OnBeforeSaving();
      return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) {
      _OnBeforeSaving();
      return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void _OnBeforeSaving() {
      var entries = ChangeTracker.Entries().ToList();
      using var sha512 = SHA512.Create();
      foreach (var x in entries.Where(x => x.Entity is CardTemplateInstanceEntity)) {
        var template = (CardTemplateInstanceEntity) x.Entity;
        template.Hash = _entityHasher.CardTemplateInstanceHasher.Invoke((template, sha512));
      }
      foreach (var x in entries.Where(x => x.Entity is CardInstanceEntity)) {
        var card = (CardInstanceEntity) x.Entity;
        var templateHash = card.CardTemplateInstance?.Hash ?? CardTemplateInstance.Find(card.CardTemplateInstanceId).Hash;
        card.Hash = _entityHasher.CardInstanceHasher.Invoke((card, templateHash, sha512));
      }
    }

  }
}
