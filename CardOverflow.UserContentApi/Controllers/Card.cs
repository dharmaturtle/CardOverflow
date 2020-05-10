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
  public class Card : Controller {
    private readonly CardOverflowDb _db;

    public Card(CardOverflowDb db) {
      _db = db;
    }

    [HttpGet("card/{id}/{index}/front")]
    public async Task<IActionResult> Front(int id, int index) => _front(index, await CardViewRepository.get(_db, id));

    [HttpGet("card/{id}/{index}/back")]
    public async Task<IActionResult> Back(int id, int index) => _back(index, await CardViewRepository.get(_db, id));

    [HttpGet("cardinstance/{id}/{index}/front")]
    public async Task<IActionResult> InstanceFront(int id, int index) => _front(index, await CardViewRepository.instance(_db, id));

    [HttpGet("cardinstance/{id}/{index}/back")]
    public async Task<IActionResult> InstanceBack(int id, int index) => _back(index, await CardViewRepository.instance(_db, id));

    private ContentResult _front(int index, FSharpResult<BranchInstanceView, string> view) =>
      ( view.IsError
      ? view.ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynthIndex(index).IsError
      ? view.ResultValue.FrontBackFrontSynthBackSynthIndex(index).ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynthIndex(index).ResultValue.Item1).ToTextHtmlContent(this);

    private ContentResult _back(int index, FSharpResult<BranchInstanceView, string> view) =>
      ( view.IsError
      ? view.ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynthIndex(index).IsError
      ? view.ResultValue.FrontBackFrontSynthBackSynthIndex(index).ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynthIndex(index).ResultValue.Item2).ToTextHtmlContent(this);

  }
}
