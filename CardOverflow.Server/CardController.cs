using System.Threading.Tasks;
using CardOverflow.Api;
using CardOverflow.Entity;
using CardOverflow.Pure;
using Microsoft.AspNetCore.Mvc;

namespace CardOverflow.Server {
  public class CardController : Controller {
    private readonly CardOverflowDb _db;

    public CardController(CardOverflowDb db) {
      _db = db;
    }

    [HttpGet("card/{id}/front")] // highTODO move to another server
    public async Task<IActionResult> Front(int id) =>
      Content((await CardRepository.getView(_db, id)).FrontBackFrontSynthBackSynth.Item1, "text/html");

    [HttpGet("card/{id}/back")] // highTODO move to another server
    public async Task<IActionResult> Back(int id) =>
      Content((await CardRepository.getView(_db, id)).FrontBackFrontSynthBackSynth.Item2, "text/html");

    [HttpGet("cardinstance/{id}/front")] // highTODO move to another server
    public async Task<IActionResult> InstanceFront(int id) {
      var x = await CardRepository.instance(_db, id);
      return (x == null ? "" : x.Value.FrontBackFrontSynthBackSynth.Item1).Apply(s => Content(s, "text/html"));
    }

    [HttpGet("cardinstance/{id}/back")] // highTODO move to another server
    public async Task<IActionResult> InstanceBack(int id) {
      var x = await CardRepository.instance(_db, id);
      return (x == null ? "" : x.Value.FrontBackFrontSynthBackSynth.Item2).Apply(s => Content(s, "text/html"));
    }

  }
}
