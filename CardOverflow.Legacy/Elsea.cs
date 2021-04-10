using System;
using Nest;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using static Domain.Projection;

public static class Elsea {
  public static class Example {

    public static async Task<IReadOnlyCollection<StackSearch>> GetUsersStack(ElasticClient client, string authorId, string exampleId) {
      var searchResponse = await client.SearchAsync<StackSearch>(s => s
        .Query(q => q
          .Bool(b => b
            .Must(mu => mu
              .Match(m => m
                .Field(f => f.AuthorId)
                .Query(authorId.ToString())
              ), mu => mu
              .Match(m => m
                .Field(f => f.ExampleId)
                .Query(exampleId.ToString())
              )
            )
          )
        )
      );
      return searchResponse.Documents;
    }

    public static async Task UpsertSearch(ElasticClient client, IDictionary<string, object> search) {
      var indexName = client.ConnectionSettings.DefaultIndices[typeof(ExampleSearch)];
      var _ = await client.UpdateAsync(
        DocumentPath<object>.Id(search[nameof(ExampleSearch.Id)].ToString()), x => x
          .Index(indexName)
          .Doc(search)
          .DocAsUpsert()
          .RetryOnConflict(5));
    }

  }
}
