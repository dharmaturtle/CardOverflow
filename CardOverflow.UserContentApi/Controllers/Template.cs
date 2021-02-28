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
  public class Template : Controller {
    private readonly CardOverflowDb _db;

    public Template(CardOverflowDb db) => _db = db;

    [HttpGet("template/{id}/{index}/front")]
    public async Task<IActionResult> Front(Guid id, int index) => _front(index, await TemplateRepository.latest(_db, id));

    [HttpGet("template/{id}/{index}/back")]
    public async Task<IActionResult> Back(Guid id, int index) => _back(index, await TemplateRepository.latest(_db, id));

    [HttpGet("templateleaf/{id}/{index}/front")]
    public async Task<IActionResult> LeafFront(Guid id, int index) => _front(index, await TemplateRepository.leaf(_db, id));

    [HttpGet("templateleaf/{id}/{index}/back")]
    public async Task<IActionResult> LeafBack(Guid id, int index) => _back(index, await TemplateRepository.leaf(_db, id));

    private ContentResult _front(int index, FSharpResult<TemplateRevision, string> view) =>
      ( view.IsError
      ? view.ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynthIndexed(index).IsError
      ? view.ResultValue.FrontBackFrontSynthBackSynthIndexed(index).ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynthIndexed(index).ResultValue.Item1
      ) .ToTextHtmlContent(this);

    private ContentResult _back(int index, FSharpResult<TemplateRevision, string> view) =>
      ( view.IsError
      ? view.ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynthIndexed(index).IsError
      ? view.ResultValue.FrontBackFrontSynthBackSynthIndexed(index).ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynthIndexed(index).ResultValue.Item2
      ) .ToTextHtmlContent(this);

  }
}
