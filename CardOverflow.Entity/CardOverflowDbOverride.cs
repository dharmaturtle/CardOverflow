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
using Npgsql;
using Npgsql.NameTranslation;

namespace CardOverflow.Entity {

  public interface IEntityHasher {
    FSharpFunc<(LeafEntity, BitArray, SHA512), BitArray> LeafHasher { get; }
    FSharpFunc<(GrompleafEntity, SHA512), BitArray> GrompleafHasher { get; }
    FSharpFunc<LeafEntity, short> GetMaxIndexInclusive { get; }
    FSharpFunc<string, string> SanitizeTag { get; }
  }

  public enum SearchOrder {
    Popularity,
    Relevance
  }

  public static class IQueryableExtensions {
    // In C# because SQL's SUM returns NULL on an empty list, so we need the ?? operator, which doesn't exist in F#. At least not one that LINQ to Entities can parse
    public static IOrderedQueryable<LeafEntity> Search(
      this IQueryable<LeafEntity> leafs,
      string searchTerm,
      string plain,
      string wildcard,
      SearchOrder searchOrder
    ) {
      const NpgsqlTsRankingNormalization normalization =
          NpgsqlTsRankingNormalization.DivideBy1PlusLogLength
        | NpgsqlTsRankingNormalization.DivideByMeanHarmonicDistanceBetweenExtents;

      IQueryable<LeafEntity> where(IQueryable<LeafEntity> query) =>
        string.IsNullOrWhiteSpace(searchTerm)
        ? query
        : query.Where(x => x.Tsv.Matches(Functions.WebSearchToTsQuery(plain).And(Functions.ToTsQuery(wildcard))));

      IOrderedQueryable<LeafEntity> order(IQueryable<LeafEntity> query) =>
        searchOrder == SearchOrder.Popularity
        ? query.OrderByDescending(x => x.Example.Users)
        : query.OrderByDescending(x =>
          x.Tsv.RankCoverDensity(Functions.WebSearchToTsQuery(plain).And(Functions.ToTsQuery(wildcard)), normalization));

      return leafs
        .Apply(where)
        .Apply(order);
    }
  }

  // This class should not store custom state due to usage of `AddDbContextPool`
  public partial class CardOverflowDb : DbContext {
    private readonly IEntityHasher _entityHasher;

    static CardOverflowDb() {
      NpgsqlConnection.GlobalTypeMapper.MapEnum<NotificationType>("notification_type", new NpgsqlNullNameTranslator());
      NpgsqlConnection.GlobalTypeMapper.MapEnum<TimezoneName>("timezone_name", new NpgsqlNullNameTranslator());
      NpgsqlConnection.GlobalTypeMapper.MapEnum<StudyOrder>("study_order", new NpgsqlNullNameTranslator());
    }

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

    public void Remove<TEntity>(Guid id) where TEntity : class, IId, new() { // https://stackoverflow.com/a/55853315
      var dbSet = Set<TEntity>();
      var entity = dbSet.Local.FirstOrDefault(c => c.Id == id);
      if (entity == null) {
        entity = new TEntity { Id = id };
        dbSet.Attach(entity);
      }
      dbSet.Remove(entity);
    }

    public void Remove<TEntity>(IList<Guid> ids) where TEntity : class, IId, new() {
      foreach (var id in ids) {
        Remove<TEntity>(id);
      }
    }

    private async Task _OnBeforeSaving() {
      var entries = ChangeTracker.Entries().ToList();
      using var sha512 = SHA512.Create();
      foreach (var gromplate in _filter<GrompleafEntity>(entries)) {
        gromplate.Hash = _entityHasher.GrompleafHasher.Invoke((gromplate, sha512));
        gromplate.CWeightTsvHelper =
          Fields.fromString.Invoke(gromplate.Fields).Select(x => x.Name)
            .Append(MappingTools.stripHtmlTags(gromplate.Templates))
            .Apply(x => string.Join(' ', x));
      }
      foreach (var leaf in _filter<LeafEntity>(entries)) {
        if (leaf.Grompleaf == null) {
          leaf.Grompleaf = await Grompleaf.FindAsync(leaf.GrompleafId);
        }
        leaf.MaxIndexInclusive = _entityHasher.GetMaxIndexInclusive.Invoke(leaf);
        var gromplateHash = leaf.Grompleaf?.Hash ?? Grompleaf.Find(leaf.GrompleafId).Hash;
        leaf.Hash = _entityHasher.LeafHasher.Invoke((leaf, gromplateHash, sha512));
        leaf.TsvHelper = MappingTools.stripHtmlTags(leaf.FieldValues);
      }
      foreach (var commeaf in _filter<CommeafEntity>(entries)) {
        commeaf.BWeightTsvHelper = MappingTools.stripHtmlTags(commeaf.Value);
      }
      foreach (var card in _filter<CardEntity>(entries)) {
        card.TsvHelper = MappingTools.stripHtmlTags(card.FrontPersonalField) + " " + MappingTools.stripHtmlTags(card.BackPersonalField);
        card.Tags = card.Tags.Select(_entityHasher.SanitizeTag.Invoke).ToArray();
      }
      foreach (var relationship in _filter<RelationshipEntity>(entries)) {
        relationship.Name = MappingTools.toTitleCase.Invoke(relationship.Name);
      }
    }

    public IQueryable<CardIsLatestEntity> CardIsLatest => _CardIsLatestTracked.AsNoTracking();
    public IQueryable<LeafRelationshipCountEntity> LeafRelationshipCount => _LeafRelationshipCountTracked.AsNoTracking();
    public IQueryable<ConceptRelationshipCountEntity> ConceptRelationshipCount => _ConceptRelationshipCountTracked.AsNoTracking();
    public IQueryable<LeafEntity> LatestLeaf => Leaf.Where(x => x.Example.LatestId == x.Id).AsNoTracking();
    public IQueryable<LeafEntity> LatestDefaultLeaf => LatestLeaf.Where(x => x.Example.Concept.DefaultExampleId == x.ExampleId).AsNoTracking();
    public IQueryable<CommeafEntity> LatestCommeaf => Commeaf.Where(x => x.Commield.LatestId == x.Id).AsNoTracking();
    public IQueryable<GrompleafEntity> LatestGrompleaf => Grompleaf.Where(x => x.Gromplate.LatestId == x.Id).AsNoTracking();

  }
}
