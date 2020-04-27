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

    public IOrderedQueryable<LatestCardInstanceEntity> SearchLatestCardInstance(string searchTerm, string plain, string wildcard, SearchOrder searchOrder) {
      var normalization = NpgsqlTsRankingNormalization.DivideBy1PlusLogLength | NpgsqlTsRankingNormalization.DivideByMeanHarmonicDistanceBetweenExtents;
      return LatestCardInstance
        .Where(x =>
          String.IsNullOrWhiteSpace(searchTerm) ||
          x.CardInstance.AcquiredCards.Any(x => x.Tag_AcquiredCards.Any(x =>
            x.Tag.TsVector.Matches(EF.Functions.WebSearchToTsQuery(plain).And(EF.Functions.ToTsQuery(wildcard))))) ||
          x.CardInstance.TsVector.Matches(EF.Functions.WebSearchToTsQuery(plain).And(EF.Functions.ToTsQuery(wildcard))))
        .OrderByDescending(x =>
          searchOrder == SearchOrder.Popularity
          ? x.CardUsers
          : x.CardInstance.TsVector.RankCoverDensity(
            EF.Functions.WebSearchToTsQuery(plain).And(EF.Functions.ToTsQuery(wildcard)), normalization) +
            ((float?) x.CardInstance.AcquiredCards.Sum(x => x.Tag_AcquiredCards.Sum(x =>
              x.Tag.TsVector.RankCoverDensity(
                EF.Functions.WebSearchToTsQuery(plain).And(EF.Functions.ToTsQuery(wildcard)), normalization))) ?? 0) / 3); // In C# because SQL's SUM returns NULL on an empty list, so we need the ?? operator, which doesn't exist in F#. At least not one that LINQ to Entities can parse
    }

    private void _OnBeforeSaving() {
      var entries = ChangeTracker.Entries().ToList();
      using var sha512 = SHA512.Create();
      foreach (var x in entries.Where(x => x.Entity is TemplateInstanceEntity)) {
        var template = (TemplateInstanceEntity) x.Entity;
        template.Hash = _entityHasher.TemplateInstanceHasher.Invoke((template, sha512));
        template.CWeightTsVectorHelper =
          Fields.fromString.Invoke(template.Fields).Select(x => x.Name)
            .Append(MappingTools.stripHtmlTags(template.QuestionTemplate))
            .Append(MappingTools.stripHtmlTags(template.AnswerTemplate))
            .Apply(x => string.Join(' ', x));
      }
      foreach (var x in entries.Where(x => x.Entity is CardInstanceEntity)) {
        var card = (CardInstanceEntity) x.Entity;
        var templateHash = card.TemplateInstance?.Hash ?? TemplateInstance.Find(card.TemplateInstanceId).Hash;
        card.Hash = _entityHasher.CardInstanceHasher.Invoke((card, templateHash, sha512));
        card.TsVectorHelper = MappingTools.stripHtmlTags(card.FieldValues);
      }
      foreach (var x in entries.Where(x => x.Entity is CommunalFieldInstanceEntity)) {
        var communalFieldInstance = (CommunalFieldInstanceEntity) x.Entity;
        communalFieldInstance.BWeightTsVectorHelper = MappingTools.stripHtmlTags(communalFieldInstance.Value);
      }
    }

    public IQueryable<AcquiredCardIsLatestEntity> AcquiredCardIsLatest => _AcquiredCardIsLatestTracked.AsNoTracking();
    public IQueryable<CardInstanceRelationshipCountEntity> CardInstanceRelationshipCount => _CardInstanceRelationshipCountTracked.AsNoTracking();
    public IQueryable<CardInstanceTagCountEntity> CardInstanceTagCount => _CardInstanceTagCountTracked.AsNoTracking();
    public IQueryable<CardRelationshipCountEntity> CardRelationshipCount => _CardRelationshipCountTracked.AsNoTracking();
    public IQueryable<CardTagCountEntity> CardTagCount => _CardTagCountTracked.AsNoTracking();
    public IQueryable<LatestCardInstanceEntity> LatestCardInstance => _LatestCardInstanceTracked.AsNoTracking();
    public IQueryable<LatestCommunalFieldInstanceEntity> LatestCommunalFieldInstance => _LatestCommunalFieldInstanceTracked.AsNoTracking();
    public IQueryable<LatestTemplateInstanceEntity> LatestTemplateInstance => _LatestTemplateInstanceTracked.AsNoTracking();

  }
}
