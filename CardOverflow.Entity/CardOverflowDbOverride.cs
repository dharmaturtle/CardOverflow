using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Microsoft.FSharp.Core;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using CardOverflow.Pure;
using static Microsoft.EntityFrameworkCore.EF;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using CardOverflow.Debug;

namespace CardOverflow.Entity {

  public enum NotificationType {
    DeckAddedBranchInstance,
    DeckUpdatedBranchInstance,
    DeckDeletedBranchInstance
  }

  public interface IEntityHasher {
    FSharpFunc<(BranchInstanceEntity, BitArray, SHA512), BitArray> BranchInstanceHasher { get; }
    FSharpFunc<(CollateInstanceEntity, SHA512), BitArray> CollateInstanceHasher { get; }
    FSharpFunc<BranchInstanceEntity, short> GetMaxIndexInclusive { get; }
    FSharpFunc<string, string> SanitizeTag { get; }
  }

  public enum SearchOrder {
    Popularity,
    Relevance
  }

  public static class IQueryableExtensions {
    // In C# because SQL's SUM returns NULL on an empty list, so we need the ?? operator, which doesn't exist in F#. At least not one that LINQ to Entities can parse
    public static IOrderedQueryable<BranchInstanceEntity> Search(
      this IQueryable<BranchInstanceEntity> instances,
      string searchTerm,
      string plain,
      string wildcard,
      SearchOrder searchOrder
    ) {
      const NpgsqlTsRankingNormalization normalization =
          NpgsqlTsRankingNormalization.DivideBy1PlusLogLength
        | NpgsqlTsRankingNormalization.DivideByMeanHarmonicDistanceBetweenExtents;

      IQueryable<BranchInstanceEntity> where(IQueryable<BranchInstanceEntity> query) =>
        string.IsNullOrWhiteSpace(searchTerm)
        ? query
        : query.Where(x =>
          x.AcquiredCards.Any(x => x.Tag_AcquiredCards.Any(x => x.Tag.TsVector.Matches(
              Functions.WebSearchToTsQuery(plain).And(Functions.ToTsQuery(wildcard)))))
            || x.TsVector.Matches(
              Functions.WebSearchToTsQuery(plain).And(Functions.ToTsQuery(wildcard))));

      IOrderedQueryable<BranchInstanceEntity> order(IQueryable<BranchInstanceEntity> query) =>
        searchOrder == SearchOrder.Popularity
        ? query.OrderByDescending(x => x.Branch.Users)
        : query.OrderByDescending(x =>
          x.TsVector.RankCoverDensity(
              Functions.WebSearchToTsQuery(plain).And(Functions.ToTsQuery(wildcard)), normalization)
          + (((float?)x.AcquiredCards.Sum(x => x.Tag_AcquiredCards.Sum(x =>
            x.Tag.TsVector.RankCoverDensity(
              Functions.WebSearchToTsQuery(plain).And(Functions.ToTsQuery(wildcard)), normalization)
            )) ?? 0) / 3)); // the division by 3 is utterly arbitrary, lowTODO find a better way to combine two TsVector's Ranks;

      return instances
        .Apply(where)
        .Apply(order);
    }
  }

  // This class should not store custom state due to usage of `AddDbContextPool`
  public partial class CardOverflowDb : DbContext {
    private readonly IEntityHasher _entityHasher;

    public CardOverflowDb(DbContextOptions<CardOverflowDb> options) : base(options) {
      _entityHasher = this.GetService<IEntityHasher>(); // lowTODO consider injecting the SHA512 hasher; it's also IDisposable
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess) {
      _OnBeforeSaving().GetAwaiter().GetResult();
      return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) {
      await _OnBeforeSaving();
      return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private IEnumerable<T> _filter<T>(List<EntityEntry> entityEntries) =>
      entityEntries.Where(x =>
        x.Entity is T
        && (x.State == EntityState.Added || x.State == EntityState.Modified)
      ).Select(x => x.Entity).Cast<T>();

    public void Remove<TEntity>(int id) where TEntity : class, IId, new() { // https://stackoverflow.com/a/55853315
      var dbSet = Set<TEntity>();
      var entity = dbSet.Local.FirstOrDefault(c => c.Id == id);
      if (entity == null) {
        entity = new TEntity { Id = id };
        dbSet.Attach(entity);
      }
      dbSet.Remove(entity);
    }

    public void Remove<TEntity>(IList<int> ids) where TEntity : class, IId, new() {
      foreach (var id in ids) {
        Remove<TEntity>(id);
      }
    }

    private async Task _OnBeforeSaving() {
      var entries = ChangeTracker.Entries().ToList();
      using var sha512 = SHA512.Create();
      foreach (var collate in _filter<CollateInstanceEntity>(entries)) {
        collate.Hash = _entityHasher.CollateInstanceHasher.Invoke((collate, sha512));
        collate.CWeightTsVectorHelper =
          Fields.fromString.Invoke(collate.Fields).Select(x => x.Name)
            .Append(MappingTools.stripHtmlTags(collate.Templates))
            .Apply(x => string.Join(' ', x));
      }
      foreach (var branchInstance in _filter<BranchInstanceEntity>(entries)) {
        if (branchInstance.CollateInstance == null) {
          branchInstance.CollateInstance = await CollateInstance.FindAsync(branchInstance.CollateInstanceId);
        }
        branchInstance.MaxIndexInclusive = _entityHasher.GetMaxIndexInclusive.Invoke(branchInstance);
        var collateHash = branchInstance.CollateInstance?.Hash ?? CollateInstance.Find(branchInstance.CollateInstanceId).Hash;
        branchInstance.Hash = _entityHasher.BranchInstanceHasher.Invoke((branchInstance, collateHash, sha512));
        branchInstance.TsVectorHelper = MappingTools.stripHtmlTags(branchInstance.FieldValues);
      }
      foreach (var communalFieldInstance in _filter<CommunalFieldInstanceEntity>(entries)) {
        communalFieldInstance.BWeightTsVectorHelper = MappingTools.stripHtmlTags(communalFieldInstance.Value);
      }
      foreach (var acquiredCard in _filter<AcquiredCardEntity>(entries)) {
        acquiredCard.TsVectorHelper = MappingTools.stripHtmlTags(acquiredCard.FrontPersonalField) + " " + MappingTools.stripHtmlTags(acquiredCard.BackPersonalField);
      }
      foreach (var tag in _filter<TagEntity>(entries)) {
        tag.Name = _entityHasher.SanitizeTag.Invoke(tag.Name);
      }
      foreach (var j in _filter<Tag_AcquiredCardEntity>(entries)) {
        if (j.AcquiredCard.StackId == 0) {
          if (j.AcquiredCard.Stack == null) {
            throw new NullReferenceException("j.AcquiredCard.Stack is null and its j.AcquiredCard.StackId is 0. In other words, the Stack wasn't set.");
          }
          j.Stack = j.AcquiredCard.Stack;
        } else {
          j.StackId = j.AcquiredCard.StackId;
        }
        j.UserId = j.AcquiredCard.UserId;
      }
      foreach (var relationship in _filter<RelationshipEntity>(entries)) {
        relationship.Name = MappingTools.toTitleCase.Invoke(relationship.Name);
      }
    }

    public IQueryable<AcquiredCardIsLatestEntity> AcquiredCardIsLatest => _AcquiredCardIsLatestTracked.AsNoTracking();
    public IQueryable<BranchInstanceRelationshipCountEntity> BranchInstanceRelationshipCount => _BranchInstanceRelationshipCountTracked.AsNoTracking();
    public IQueryable<BranchInstanceTagCountEntity> BranchInstanceTagCount => _BranchInstanceTagCountTracked.AsNoTracking();
    public IQueryable<StackRelationshipCountEntity> StackRelationshipCount => _StackRelationshipCountTracked.AsNoTracking();
    public IQueryable<StackTagCountEntity> StackTagCount => _StackTagCountTracked.AsNoTracking();
    public IQueryable<BranchInstanceEntity> LatestBranchInstance => BranchInstance.Where(x => x.Branch.LatestInstanceId == x.Id).AsNoTracking();
    public IQueryable<BranchInstanceEntity> LatestDefaultBranchInstance => LatestBranchInstance.Where(x => x.Branch.Stack.DefaultBranchId == x.BranchId).AsNoTracking();
    public IQueryable<CommunalFieldInstanceEntity> LatestCommunalFieldInstance => CommunalFieldInstance.Where(x => x.CommunalField.LatestInstanceId == x.Id).AsNoTracking();
    public IQueryable<CollateInstanceEntity> LatestCollateInstance => CollateInstance.Where(x => x.Collate.LatestInstanceId == x.Id).AsNoTracking();

  }
}
