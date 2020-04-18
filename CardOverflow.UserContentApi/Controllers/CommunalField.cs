using System.Threading.Tasks;
using CardOverflow.Api;
using CardOverflow.Entity;
using CardOverflow.Pure;
using Microsoft.AspNetCore.Mvc;

namespace CardOverflow.UserContentApi.Controllers {
  public class CommunalField : Controller {
    private readonly CardOverflowDb _db;

    public CommunalField(CardOverflowDb db) {
      _db = db;
    }

    [HttpGet("communalfield/{id}")]
    public async Task<IActionResult> Get(int id) =>
      Content(await CommunalFieldRepository.get(_db, id), "text/html");

    [HttpGet("communalfieldinstance/{id}")]
    public async Task<IActionResult> GetInstance(int id) =>
      Content(await CommunalFieldRepository.getInstance(_db, id), "text/html");

  }
}
