using System.Threading.Tasks;
using CardOverflow.Api;
using CardOverflow.Entity;
using CardOverflow.Pure;
using Microsoft.AspNetCore.Mvc;
using ThoughtDesign.WebLibrary;

namespace CardOverflow.UserContentApi.Controllers {
  [ApiController]
  //[Route("[controller]")]
  public class CommunalField : Controller {
    private readonly CardOverflowDb _db;

    public CommunalField(CardOverflowDb db) => _db = db;

    [HttpGet("communalfield/{id}")]
    public async Task<IActionResult> Get(int id) => (await CommunalFieldRepository.get(_db, id)).ToTextHtmlContent(this);

    [HttpGet("communalfieldinstance/{id}")]
    public async Task<IActionResult> GetInstance(int id) => (await CommunalFieldRepository.getInstance(_db, id)).ToTextHtmlContent(this);

  }
}
