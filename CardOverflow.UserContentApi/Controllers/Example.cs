using System;
using System.Threading.Tasks;
using CardOverflow.Api;
using CardOverflow.Entity;
using CardOverflow.Pure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FSharp.Core;
using ThoughtDesign.WebLibrary;

namespace CardOverflow.UserContentApi.Controllers {
  [ApiController]
  //[Route("[controller]")]
  public class Example : Controller {
    private readonly CardOverflowDb _db;

    public Example(CardOverflowDb db) {
      _db = db;
    }

    [HttpGet("example/{id}/{index}/front")]
    public async Task<IActionResult> Front(Guid id, int index) => _Front(index, await ConceptViewRepository.get(_db, id));

    [HttpGet("example/{id}/{index}/back")]
    public async Task<IActionResult> Back(Guid id, int index) => _Back(index, await ConceptViewRepository.get(_db, id));

    [HttpGet("revision/{id}/{index}/front")]
    public async Task<IActionResult> RevisionFront(Guid id, int index) => _Front(index, await ConceptViewRepository.revision(_db, id));

    [HttpGet("revision/{id}/{index}/back")]
    public async Task<IActionResult> RevisionBack(Guid id, int index) => _Back(index, await ConceptViewRepository.revision(_db, id));

    private ContentResult _Front(int index, FSharpResult<RevisionView, string> view) =>
      ( view.IsError
      ? view.ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynthIndex(index).IsError
      ? view.ResultValue.FrontBackFrontSynthBackSynthIndex(index).ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynthIndex(index).ResultValue.Item1).ToTextHtmlContent(this);

    private ContentResult _Back(int index, FSharpResult<RevisionView, string> view) =>
      ( view.IsError
      ? view.ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynthIndex(index).IsError
      ? view.ResultValue.FrontBackFrontSynthBackSynthIndex(index).ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynthIndex(index).ResultValue.Item2).ToTextHtmlContent(this);

  }
}
