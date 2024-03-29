using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CardOverflow.Debug;
using Microsoft.FSharp.Core;
using Nest;
using static Domain.Projection;
using static Domain.Infrastructure;
using CardOverflow.Pure;

namespace CardOverflow.Legacy {
  public static class Elsea {

    public static class MapStringStringConverter {
      public const string KeysPropertyName = "keys"; // do not change these values without regenerating elasticsearch's indexes
      public const string ValuesPropertyName = "values";
    }

    public static class Example {

      // elasticsearch wants camelcase
      private static string FirstCharacterToLower(string str) =>
        $"{char.ToLowerInvariant(str[0])}{str[1..]}";

      private readonly static string fieldValues_values =
        $"{FirstCharacterToLower(nameof(ExampleSearch.FieldValues))}.{MapStringStringConverter.ValuesPropertyName}";

      public static async Task<PagedList<ExampleSearch>> Search(IElasticClient client, string query, int pageNumber) {
        var size = 10;
        var from = (pageNumber - 1) * size;
        var searchResponse = await client.SearchAsync<ExampleSearch>(s => s
          .From(from)
          .Size(size)
          .Query(q => q
            .MultiMatch(m => m
              .Fields(fs => fs
                .Field(f => f.Title, 3)
                .Field(fieldValues_values))
              .Query(query))));
        return PagedList.create(searchResponse.Documents, pageNumber, searchResponse.Total, size);
      }

      public static async Task UpsertSearch(IElasticClient client, string exampleId, IDictionary<string, object> search) {
        var indexName = client.ConnectionSettings.DefaultIndices[typeof(ExampleSearch)];
        var _ = await client.UpdateAsync(
          DocumentPath<object>.Id(exampleId), x => x
            .Index(indexName)
            .Doc(search)
            .DocAsUpsert()
            .RetryOnConflict(5));
      }

      public static Task SetCollected(IElasticClient client, string exampleId, int collected) {
        var search = new Dictionary<string, object>() { { nameof(ExampleSearch.Collectors), collected } };
        return UpsertSearch(client, exampleId, search);
      }

    }

    public static class Template {

      public static async Task<PagedList<TemplateSearch>> Search(IElasticClient client, string query, int pageNumber) {
        var size = 10;
        var from = (pageNumber - 1) * size;
        var searchResponse = await client.SearchAsync<TemplateSearch>(s => s
          .From(from)
          .Size(size)
          .Query(q => q
            .MultiMatch(m => m
              .Fields(fs => fs
                .Field(f => f.Name))
              .Query(query))));
        return PagedList.create(searchResponse.Documents, pageNumber, searchResponse.Total, size);
      }

      public static async Task UpsertSearch(IElasticClient client, string templateId, IDictionary<string, object> search) {
        var indexName = client.ConnectionSettings.DefaultIndices[typeof(TemplateSearch)];
        var _ = await client.UpdateAsync(
          DocumentPath<object>.Id(templateId), x => x
            .Index(indexName)
            .Doc(search)
            .DocAsUpsert()
            .RetryOnConflict(5));
      }

      public static Task SetCollected(IElasticClient client, string templateId, int collected) {
        var search = new Dictionary<string, object>() { { nameof(TemplateSearch.Collectors), collected } };
        return UpsertSearch(client, templateId, search);
      }

    }

    public static class Deck {

      public static async Task<PagedList<DeckSearch>> Search(IElasticClient client, string query, int pageNumber) {
        var size = 10;
        var from = (pageNumber - 1) * size;
        var searchResponse = await client.SearchAsync<DeckSearch>(s => s
          .From(from)
          .Size(size)
          .Query(q => q
            .MultiMatch(m => m
              .Fields(fs => fs
                .Field(f => f.Name, 3)
                .Field(f => f.Description))
              .Query(query))));
        return PagedList.create(searchResponse.Documents, pageNumber, searchResponse.Total, size);
      }

      public static async Task UpsertSearch(IElasticClient client, string deckId, IDictionary<string, object> search) {
        var indexName = client.ConnectionSettings.DefaultIndices[typeof(DeckSearch)];
        var _ = await client.UpdateAsync(
          DocumentPath<object>.Id(deckId), x => x
            .Index(indexName)
            .Doc(search)
            .DocAsUpsert()
            .RetryOnConflict(5));
      }

      public static Task SetExampleCount(IElasticClient client, string deckId, int exampleCount) {
        var search = new Dictionary<string, object>() { { nameof(DeckSearch.ExampleCount), exampleCount } };
        return UpsertSearch(client, deckId, search);
      }

    }

  }
}