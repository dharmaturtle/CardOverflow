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

namespace CardOverflow.Entity {

  public interface IEntityHasher {
    FSharpFunc<(CardInstanceEntity, BitArray, SHA512), BitArray> CardInstanceHasher { get; }
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
    public IOrderedQueryable<CardInstanceEntity> SearchLatestCardInstance(
      string searchTerm,
      string plain,
      string wildcard,
      SearchOrder searchOrder
    ) {
      const NpgsqlTsRankingNormalization normalization =
          NpgsqlTsRankingNormalization.DivideBy1PlusLogLength
        | NpgsqlTsRankingNormalization.DivideByMeanHarmonicDistanceBetweenExtents;

      IQueryable<CardInstanceEntity> where(IQueryable<CardInstanceEntity> query) =>
        String.IsNullOrWhiteSpace(searchTerm)
        ? query
        : query.Where(x =>
          x.AcquiredCards.Any(x => x.Tag_AcquiredCards.Any(x => x.Tag.TsVector.Matches(
              Functions.WebSearchToTsQuery(plain).And(Functions.ToTsQuery(wildcard)))))
            || x.TsVector.Matches(
              Functions.WebSearchToTsQuery(plain).And(Functions.ToTsQuery(wildcard))));

      IOrderedQueryable<CardInstanceEntity> order(IQueryable<CardInstanceEntity> query) =>
        searchOrder == SearchOrder.Popularity
        ? query.OrderByDescending(x => x.Card.Users)
        : query.OrderByDescending(x =>
          x.TsVector.RankCoverDensity(
              Functions.WebSearchToTsQuery(plain).And(Functions.ToTsQuery(wildcard)), normalization)
          + (((float?)x.AcquiredCards.Sum(x => x.Tag_AcquiredCards.Sum(x =>
            x.Tag.TsVector.RankCoverDensity(
              Functions.WebSearchToTsQuery(plain).And(Functions.ToTsQuery(wildcard)), normalization)
            )) ?? 0) / 3)); // the division by 3 is utterly arbitrary, lowTODO find a better way to combine two TsVector's Ranks;

      return LatestCardInstance
        .Apply(where)
        .Apply(order);
    }

    private void _OnBeforeSaving() {
      var entries = ChangeTracker.Entries().ToList();
      using var sha512 = SHA512.Create();
      foreach (var x in entries.Where(x => x.Entity is TemplateInstanceEntity)) {
        var template = (TemplateInstanceEntity)x.Entity;
        template.Hash = _entityHasher.TemplateInstanceHasher.Invoke((template, sha512));
        template.CWeightTsVectorHelper =
          Fields.fromString.Invoke(template.Fields).Select(x => x.Name)
            .Append(MappingTools.stripHtmlTags(template.QuestionTemplate))
            .Append(MappingTools.stripHtmlTags(template.AnswerTemplate))
            .Apply(x => string.Join(' ', x));
      }
      foreach (var x in entries.Where(x => x.Entity is CardInstanceEntity)) {
        var card = (CardInstanceEntity)x.Entity;
        var templateHash = card.TemplateInstance?.Hash ?? TemplateInstance.Find(card.TemplateInstanceId).Hash;
        card.Hash = _entityHasher.CardInstanceHasher.Invoke((card, templateHash, sha512));
        card.TsVectorHelper = MappingTools.stripHtmlTags(card.FieldValues);
      }
      foreach (var x in entries.Where(x => x.Entity is CommunalFieldInstanceEntity)) {
        var communalFieldInstance = (CommunalFieldInstanceEntity)x.Entity;
        communalFieldInstance.BWeightTsVectorHelper = MappingTools.stripHtmlTags(communalFieldInstance.Value);
      }
    }

    public IQueryable<AcquiredCardIsLatestEntity> AcquiredCardIsLatest => _AcquiredCardIsLatestTracked.AsNoTracking();
    public IQueryable<CardInstanceRelationshipCountEntity> CardInstanceRelationshipCount => _CardInstanceRelationshipCountTracked.AsNoTracking();
    public IQueryable<CardInstanceTagCountEntity> CardInstanceTagCount => _CardInstanceTagCountTracked.AsNoTracking();
    public IQueryable<CardRelationshipCountEntity> CardRelationshipCount => _CardRelationshipCountTracked.AsNoTracking();
    public IQueryable<CardTagCountEntity> CardTagCount => _CardTagCountTracked.AsNoTracking();
    public IQueryable<CardInstanceEntity> LatestCardInstance => CardInstance.Where(x => x.Card.LatestInstanceId == x.Id).AsNoTracking();
    public IQueryable<LatestCommunalFieldInstanceEntity> LatestCommunalFieldInstance => _LatestCommunalFieldInstanceTracked.AsNoTracking();
    public IQueryable<LatestTemplateInstanceEntity> LatestTemplateInstance => _LatestTemplateInstanceTracked.AsNoTracking();

  }
}
