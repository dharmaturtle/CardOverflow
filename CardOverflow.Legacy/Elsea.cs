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

    public static async Task UpsertSearch(ElasticClient client, ExampleSearch search) {
      var _ = await client.UpdateAsync(
        DocumentPath<ExampleSearch>.Id(search.Id), x => x
          .Doc(search)
          .DocAsUpsert()
          .RetryOnConflict(5));
    }

  }
}
