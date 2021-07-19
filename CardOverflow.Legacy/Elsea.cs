using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CardOverflow.Debug;
using Microsoft.FSharp.Core;
using Nest;
using static Domain.Projection;
using static Domain.Infrastructure;

public static class Elsea {

  public static class Example {

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
