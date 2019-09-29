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
    public async Task<IActionResult> GetFront(int id) =>
      Content((await CardRepository.Get(_db, id, 0)).LatestInstance.FrontBackFrontSynthBackSynth.Item1, "text/html");

    [HttpGet("card/rawback/{id}")] // highTODO move to another server
    public async Task<IActionResult> GetBack(int id) =>
      Content((await CardRepository.Get(_db, id, 0)).LatestInstance.FrontBackFrontSynthBackSynth.Item2, "text/html");

  }
}
