using System.Threading.Tasks;
using CardOverflow.Api;
using CardOverflow.Entity;
using CardOverflow.Pure;
using Microsoft.AspNetCore.Mvc;

namespace CardOverflow.Server {
  public class CommunalFieldController : Controller {
    private readonly CardOverflowDb _db;

    public CommunalFieldController(CardOverflowDb db) {
      _db = db;
    }

    [HttpGet("communalfield/{id}")] // highTODO move to another server
    public async Task<IActionResult> Get(int id) =>
      Content(await CommunalFieldRepository.get(_db, id), "text/html");

    [HttpGet("communalfieldinstance/{id}")] // highTODO move to another server
    public async Task<IActionResult> GetInstance(int id) =>
      Content(await CommunalFieldRepository.getInstance(_db, id), "text/html");

  }
}
