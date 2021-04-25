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

  public static class Stack {

    public static async Task<IReadOnlyCollection<StackSearch>> Get(ElasticClient client, string authorId, string exampleId) {
      var searchResponse = await client.SearchAsync<StackSearch>(s => s
        .Query(q => q
          .Bool(b => b
            .Must(mu => mu
              .Match(m => m
                .Field(f => f.AuthorId)
                .Query(authorId)
              ), mu => mu
              .Match(m => m
                .Field(f => f.ExampleId)
                .Query(exampleId)
              )
            )
          )
        )
      );
      return searchResponse.Documents;
    }

    public static async Task UpsertSearch(ElasticClient client, string stackId, IDictionary<string, object> doc) {
      var indexName = client.ConnectionSettings.DefaultIndices[typeof(StackSearch)];
      var _ = await client.UpdateAsync(
        DocumentPath<object>.Id(stackId), x => x
          .Index(indexName)
          .Doc(doc)
          .DocAsUpsert()
          .RetryOnConflict(5));
    }

  }

  public static class Example {

    public static async Task UpsertSearch(ElasticClient client, string exampleId, IDictionary<string, object> search) {
      var indexName = client.ConnectionSettings.DefaultIndices[typeof(ExampleSearch)];
      var _ = await client.UpdateAsync(
        DocumentPath<object>.Id(exampleId), x => x
          .Index(indexName)
          .Doc(search)
          .DocAsUpsert()
          .RetryOnConflict(5));
    }

    const string revisionIdByCollectorId = "revisionIdByCollectorId";

    public static async Task HandleCollected(ElasticClient client, ExampleSearch_OnCollected onCollected) {
      var _ = await client.UpdateAsync(
        DocumentPath<ExampleSearch>.Id(onCollected.ExampleId.ToString()), u => u
          .Script(s => s
            .Source(@$"
if (ctx._source.{revisionIdByCollectorId} == null)
  ctx._source.{revisionIdByCollectorId} = params;
else
  ctx._source.{revisionIdByCollectorId}.putAll(params);")
            .Params(p => p.Add(
              onCollected.CollectorId.ToString(),
              onCollected.RevisionId.ToString())))
          .ScriptedUpsert()
          .RetryOnConflict(5));
    }

    public static async Task HandleDiscarded(ElasticClient client, ExampleSearch_OnDiscarded onDiscarded) {
      const string discarderIdKey = "discarderId";
      var _ = await client.UpdateAsync(
        DocumentPath<ExampleSearch>.Id(onDiscarded.ExampleId.ToString()), u => u
          .Script(s => s
            .Source(@$"
if (ctx._source.{revisionIdByCollectorId} != null)
  ctx._source.{revisionIdByCollectorId}.remove(params.{discarderIdKey});")
            .Params(p => p.Add(
              discarderIdKey,
              onDiscarded.DiscarderId.ToString())))
          .RetryOnConflict(5));
    }

    /*
     medTODO: don't query for "Collected" - store the collected revision ids clientside, then use that to control the Collected field's value
     
     https://www.elastic.co/guide/en/elasticsearch/reference/current/search-fields.html
     
     It�s important to understand the difference between doc['my_field'].value and params['_source']['my_field'].
     The first, using the doc keyword, will cause the terms for that field to be loaded to memory (cached), which
     will result in faster execution, but more memory consumption. Also, the doc[...] notation only allows for simple
     valued fields (you can�t return a json object from it) and makes sense only for non-analyzed or single term
     based fields. However, using doc is still the recommended way to access values from the document, if at all
     possible, because _source must be loaded and parsed every time it�s used.
    
     *** Using _source is very slow. ***
     
    */
    public static async Task<FSharpOption<ExampleSearch>> GetFor(ElasticClient client, string callerId, string exampleId) {
      const string callerIdKey = "callerId";
      const string collectedKey = "collected";
      var searchRequest = new SearchRequest {
        Query = new IdsQuery {
          Values = new List<Id> { exampleId },
        },
        Source = new SourceFilter {
          Excludes = revisionIdByCollectorId
        },
        ScriptFields = new ScriptFields(
          new Dictionary<string, IScriptField> { {
            collectedKey,
            new ScriptField {
              Script = new InlineScript($"params._source.{revisionIdByCollectorId}[params.{callerIdKey}]") {
                Params = new Dictionary<string, object> {
                  { callerIdKey, callerId }
                }
              }
            }
          } }
        )
      };
      var response = await client.SearchAsync<ExampleSearch>(searchRequest);
      var exampleSearch = response.Hits.SingleOrDefault()?.Source;
      var opt_Collected = response.Hits.SingleOrDefault()?.Fields.Single(x => x.Key == collectedKey).Value.As<string[]>().Single();
      if (int.TryParse(opt_Collected, out var collected)) {
        exampleSearch.Collected = collected;
      }
      return OptionModule.OfObj(exampleSearch);
    }

  }

  public static class Template {

    public static async Task UpsertSearch(ElasticClient client, string templateId, IDictionary<string, object> search) {
      var indexName = client.ConnectionSettings.DefaultIndices[typeof(TemplateSearch)];
      var _ = await client.UpdateAsync(
        DocumentPath<object>.Id(templateId), x => x
          .Index(indexName)
          .Doc(search)
          .DocAsUpsert()
          .RetryOnConflict(5));
    }

    const string revisionIdByCollectorId = "revisionIdByCollectorId";

    public static async Task HandleCollected(ElasticClient client, TemplateSearch_OnCollected onCollected) {
      var _ = await client.UpdateAsync(
        DocumentPath<TemplateSearch>.Id(onCollected.TemplateId.ToString()), u => u
          .Script(s => s
            .Source(@$"
if (ctx._source.{revisionIdByCollectorId} == null)
  ctx._source.{revisionIdByCollectorId} = params;
else
  ctx._source.{revisionIdByCollectorId}.putAll(params);")
            .Params(p => p.Add(
              onCollected.CollectorId.ToString(),
              onCollected.Revision.ToString())))
          .ScriptedUpsert()
          .RetryOnConflict(5));
    }

    public static async Task HandleDiscarded(ElasticClient client, TemplateSearch_OnDiscarded onDiscarded) {
      const string discarderIdKey = "discarderId";
      var _ = await client.UpdateAsync(
        DocumentPath<TemplateSearch>.Id(onDiscarded.TemplateId.ToString()), u => u
          .Script(s => s
            .Source(@$"
if (ctx._source.{revisionIdByCollectorId} != null)
  ctx._source.{revisionIdByCollectorId}.remove(params.{discarderIdKey});")
            .Params(p => p.Add(
              discarderIdKey,
              onDiscarded.DiscarderId.ToString())))
          .RetryOnConflict(5));
    }

    /*
     medTODO: don't query for "Collected" - store the collected revision ids clientside, then use that to control the Collected field's value
     
     https://www.elastic.co/guide/en/elasticsearch/reference/current/search-fields.html
     
     It�s important to understand the difference between doc['my_field'].value and params['_source']['my_field'].
     The first, using the doc keyword, will cause the terms for that field to be loaded to memory (cached), which
     will result in faster execution, but more memory consumption. Also, the doc[...] notation only allows for simple
     valued fields (you can�t return a json object from it) and makes sense only for non-analyzed or single term
     based fields. However, using doc is still the recommended way to access values from the document, if at all
     possible, because _source must be loaded and parsed every time it�s used.
    
     *** Using _source is very slow. ***
     
    */
    public static async Task<FSharpOption<TemplateSearch>> GetFor(ElasticClient client, string callerId, string templateId) {
      const string callerIdKey = "callerId";
      const string collectedKey = "collected";
      var searchRequest = new SearchRequest {
        Query = new IdsQuery {
          Values = new List<Id> { templateId },
        },
        Source = new SourceFilter {
          Excludes = revisionIdByCollectorId
        },
        ScriptFields = new ScriptFields(
          new Dictionary<string, IScriptField> { {
            collectedKey,
            new ScriptField {
              Script = new InlineScript($"params._source.{revisionIdByCollectorId}[params.{callerIdKey}]") {
                Params = new Dictionary<string, object> {
                  { callerIdKey, callerId }
                }
              }
            }
          } }
        )
      };
      var response = await client.SearchAsync<TemplateSearch>(searchRequest);
      var templateSearch = response.Hits.SingleOrDefault()?.Source;
      var opt_Collected = response.Hits.SingleOrDefault()?.Fields.Single(x => x.Key == collectedKey).Value.As<string[]>().Single();
      if (int.TryParse(opt_Collected, out var collected)) {
        templateSearch.Collected = collected;
      }
      return OptionModule.OfObj(templateSearch);
    }

  }

}
