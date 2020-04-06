using System.Threading.Tasks;
using CardOverflow.Api;
using CardOverflow.Entity;
using CardOverflow.Pure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FSharp.Core;

namespace CardOverflow.Server {
  public class CardController : Controller {
    private readonly CardOverflowDb _db;

    public CardController(CardOverflowDb db) {
      _db = db;
    }

    [HttpGet("card/{id}/front")] // highTODO move to another server
    public async Task<IActionResult> Front(int id) => _front(await CardViewRepository.get(_db, id));

    [HttpGet("card/{id}/back")] // highTODO move to another server
    public async Task<IActionResult> Back(int id) => _back(await CardViewRepository.get(_db, id));

    [HttpGet("cardinstance/{id}/front")] // highTODO move to another server
    public async Task<IActionResult> InstanceFront(int id) => _front(await CardViewRepository.instance(_db, id));

    [HttpGet("cardinstance/{id}/back")] // highTODO move to another server
    public async Task<IActionResult> InstanceBack(int id) => _back(await CardViewRepository.instance(_db, id));

    private ContentResult _front(FSharpResult<CardInstanceView, string> view) =>
      (view.IsError ? view.ErrorValue : view.ResultValue.FrontBackFrontSynthBackSynth.Item1).Apply(_toTextHtmlContent);

    private ContentResult _back(FSharpResult<CardInstanceView, string> view) =>
      (view.IsError ? view.ErrorValue : view.ResultValue.FrontBackFrontSynthBackSynth.Item2).Apply(_toTextHtmlContent);

    private ContentResult _toTextHtmlContent(string s) => Content(s, "text/html");

  }
}
