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

    [HttpGet("collate/{id}/front")]
    public async Task<IActionResult> Front(int id) => _front(await CollateRepository.latest(_db, id));

    [HttpGet("collate/{id}/back")]
    public async Task<IActionResult> Back(int id) => _back(await CollateRepository.latest(_db, id));

    [HttpGet("collateinstance/{id}/front")]
    public async Task<IActionResult> InstanceFront(int id) => _front(await CollateRepository.instance(_db, id));

    [HttpGet("collateinstance/{id}/back")]
    public async Task<IActionResult> InstanceBack(int id) => _back(await CollateRepository.instance(_db, id));

    private ContentResult _front(FSharpResult<CollateInstance, string> view) =>
      (view.IsError ? view.ErrorValue : view.ResultValue.FrontBackFrontSynthBackSynth.Item1).ToTextHtmlContent(this);

    private ContentResult _back(FSharpResult<CollateInstance, string> view) =>
      (view.IsError ? view.ErrorValue : view.ResultValue.FrontBackFrontSynthBackSynth.Item2).ToTextHtmlContent(this);

  }
}
