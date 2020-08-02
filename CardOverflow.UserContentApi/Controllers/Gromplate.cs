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
  public class Gromplate : Controller {
    private readonly CardOverflowDb _db;

    public Gromplate(CardOverflowDb db) => _db = db;

    [HttpGet("gromplate/{id}/{index}/front")]
    public async Task<IActionResult> Front(int id, int index) => _front(index, await GromplateRepository.latest(_db, id));

    [HttpGet("gromplate/{id}/{index}/back")]
    public async Task<IActionResult> Back(int id, int index) => _back(index, await GromplateRepository.latest(_db, id));

    [HttpGet("gromplateinstance/{id}/{index}/front")]
    public async Task<IActionResult> InstanceFront(int id, int index) => _front(index, await GromplateRepository.leaf(_db, id));

    [HttpGet("gromplateinstance/{id}/{index}/back")]
    public async Task<IActionResult> InstanceBack(int id, int index) => _back(index, await GromplateRepository.leaf(_db, id));

    private ContentResult _front(int index, FSharpResult<Grompleaf, string> view) =>
      ( view.IsError
      ? view.ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynthIndexed(index).IsError
      ? view.ResultValue.FrontBackFrontSynthBackSynthIndexed(index).ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynthIndexed(index).ResultValue.Item1
      ) .ToTextHtmlContent(this);

    private ContentResult _back(int index, FSharpResult<Grompleaf, string> view) =>
      ( view.IsError
      ? view.ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynthIndexed(index).IsError
      ? view.ResultValue.FrontBackFrontSynthBackSynthIndexed(index).ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynthIndexed(index).ResultValue.Item2
      ) .ToTextHtmlContent(this);

  }
}
