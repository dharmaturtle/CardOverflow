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
  public class Collate : Controller {
    private readonly CardOverflowDb _db;

    public Collate(CardOverflowDb db) => _db = db;

    [HttpGet("collate/{id}/{index}/front")]
    public async Task<IActionResult> Front(int id, int index) => _front(index, await CollateRepository.latest(_db, id));

    [HttpGet("collate/{id}/{index}/back")]
    public async Task<IActionResult> Back(int id, int index) => _back(index, await CollateRepository.latest(_db, id));

    [HttpGet("collateinstance/{id}/{index}/front")]
    public async Task<IActionResult> InstanceFront(int id, int index) => _front(index, await CollateRepository.instance(_db, id));

    [HttpGet("collateinstance/{id}/{index}/back")]
    public async Task<IActionResult> InstanceBack(int id, int index) => _back(index, await CollateRepository.instance(_db, id));

    private ContentResult _front(int index, FSharpResult<CollateInstance, string> view) =>
      ( view.IsError
      ? view.ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynth(index).IsError
      ? view.ResultValue.FrontBackFrontSynthBackSynth(index).ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynth(index).ResultValue.Item1
      ) .ToTextHtmlContent(this);

    private ContentResult _back(int index, FSharpResult<CollateInstance, string> view) =>
      ( view.IsError
      ? view.ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynth(index).IsError
      ? view.ResultValue.FrontBackFrontSynthBackSynth(index).ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynth(index).ResultValue.Item2
      ) .ToTextHtmlContent(this);

  }
}
