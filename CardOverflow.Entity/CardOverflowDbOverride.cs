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

  public interface IEntityHasher {
    FSharpFunc<(BranchInstanceEntity, BitArray, SHA512), BitArray> BranchInstanceHasher { get; }
    FSharpFunc<(TemplateInstanceEntity, SHA512), BitArray> TemplateInstanceHasher { get; }
  }

  public enum SearchOrder {
    Popularity,
    Relevance
  }

  // This class should not store custom state due to usage of `AddDbContextPool`
  public partial class CardOverflowDb : DbContext {
    private readonly IEntityHasher _entityHasher;

    public CardOverflowDb(DbContextOptions<CardOverflowDb> options) : base(options) {
      _entityHasher = this.GetService<IEntityHasher>(); // lowTODO consider injecting the SHA512 hasher; it's also IDisposable
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess) {
      _OnBeforeSaving();
      return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) {
      _OnBeforeSaving();
      return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // In C# because SQL's SUM returns NULL on an empty list, so we need the ?? operator, which doesn't exist in F#. At least not one that LINQ to Entities can parse
    public IOrderedQueryable<BranchInstanceEntity> SearchLatestBranchInstance(
      string searchTerm,
      string plain,
      string wildcard,
      SearchOrder searchOrder
    ) {
      const NpgsqlTsRankingNormalization normalization =
          NpgsqlTsRankingNormalization.DivideBy1PlusLogLength
        | NpgsqlTsRankingNormalization.DivideByMeanHarmonicDistanceBetweenExtents;

      IQueryable<BranchInstanceEntity> where(IQueryable<BranchInstanceEntity> query) =>
        String.IsNullOrWhiteSpace(searchTerm)
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

      return LatestBranchInstance
        .Apply(where)
        .Apply(order);
    }

    private IEnumerable<T> _filter<T>(List<EntityEntry> entityEntries) =>
      entityEntries.Where(x =>
        x.Entity is T
        && (x.State == EntityState.Added || x.State == EntityState.Modified)
      ).Select(x => x.Entity).Cast<T>();

    private void _OnBeforeSaving() {
      var entries = ChangeTracker.Entries().ToList();
      using var sha512 = SHA512.Create();
      foreach (var template in _filter<TemplateInstanceEntity>(entries)) {
        template.Hash = _entityHasher.TemplateInstanceHasher.Invoke((template, sha512));
        template.CWeightTsVectorHelper =
          Fields.fromString.Invoke(template.Fields).Select(x => x.Name)
            .Append(MappingTools.stripHtmlTags(template.QuestionTemplate))
            .Append(MappingTools.stripHtmlTags(template.AnswerTemplate))
            .Apply(x => string.Join(' ', x));
      }
      foreach (var card in _filter<BranchInstanceEntity>(entries)) {
        var templateHash = card.TemplateInstance?.Hash ?? TemplateInstance.Find(card.TemplateInstanceId).Hash;
        card.Hash = _entityHasher.BranchInstanceHasher.Invoke((card, templateHash, sha512));
        card.TsVectorHelper = MappingTools.stripHtmlTags(card.FieldValues);
      }
      foreach (var communalFieldInstance in _filter<CommunalFieldInstanceEntity>(entries)) {
        communalFieldInstance.BWeightTsVectorHelper = MappingTools.stripHtmlTags(communalFieldInstance.Value);
      }
      foreach (var acquiredCard in _filter<AcquiredCardEntity>(entries)) {
        acquiredCard.TsVectorHelper = MappingTools.stripHtmlTags(acquiredCard.PersonalField);
      }
      foreach (var tag in _filter<TagEntity>(entries)) {
        tag.Name = MappingTools.toTitleCase.Invoke(tag.Name);
      }
      foreach (var relationship in _filter<RelationshipEntity>(entries)) {
        relationship.Name = MappingTools.toTitleCase.Invoke(relationship.Name);
      }
    }

    public IQueryable<AcquiredCardIsLatestEntity> AcquiredCardIsLatest => _AcquiredCardIsLatestTracked.AsNoTracking();
    public IQueryable<BranchInstanceRelationshipCountEntity> BranchInstanceRelationshipCount => _BranchInstanceRelationshipCountTracked.AsNoTracking();
    public IQueryable<BranchInstanceTagCountEntity> BranchInstanceTagCount => _BranchInstanceTagCountTracked.AsNoTracking();
    public IQueryable<CardRelationshipCountEntity> CardRelationshipCount => _CardRelationshipCountTracked.AsNoTracking();
    public IQueryable<CardTagCountEntity> CardTagCount => _CardTagCountTracked.AsNoTracking();
    public IQueryable<BranchInstanceEntity> LatestBranchInstance => BranchInstance.Where(x => x.Branch.LatestInstanceId == x.Id).AsNoTracking();
    public IQueryable<BranchInstanceEntity> LatestDefaultBranchInstance => LatestBranchInstance.Where(x => x.Branch.Card.DefaultBranchId == x.BranchId).AsNoTracking();
    public IQueryable<CommunalFieldInstanceEntity> LatestCommunalFieldInstance => CommunalFieldInstance.Where(x => x.CommunalField.LatestInstanceId == x.Id).AsNoTracking();
    public IQueryable<TemplateInstanceEntity> LatestTemplateInstance => TemplateInstance.Where(x => x.Template.LatestInstanceId == x.Id).AsNoTracking();

  }
}
