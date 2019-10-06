using System.Threading.Tasks;
using CardOverflow.Api;
using CardOverflow.Entity;
using Microsoft.AspNetCore.Mvc;

namespace CardOverflow.Server {
  public class CardController : Controller {
    private readonly CardOverflowDb _db;

    public CardController(CardOverflowDb db) {
      _db = db;
    }

    [HttpGet("card/rawfront/{id}")] // highTODO move to another server
    public async Task<IActionResult> Front(int id) =>
      Content((await CardRepository.Get(_db, id, 0)).LatestInstance.FrontBackFrontSynthBackSynth.Item1, "text/html");

    [HttpGet("card/rawback/{id}")] // highTODO move to another server
    public async Task<IActionResult> Back(int id) =>
      Content((await CardRepository.Get(_db, id, 0)).LatestInstance.FrontBackFrontSynthBackSynth.Item2, "text/html");

    [HttpGet("cardinstance/{id}/front")] // highTODO move to another server
    public async Task<IActionResult> InstanceFront(int id) =>
      Content((await CardRepository.instance(_db, 0, id)).FrontBackFrontSynthBackSynth.Item1, "text/html");

    [HttpGet("cardinstance/{id}/back")] // highTODO move to another server
    public async Task<IActionResult> InstanceBack(int id) =>
      Content((await CardRepository.instance(_db, 0, id)).FrontBackFrontSynthBackSynth.Item2, "text/html");

  }
}
