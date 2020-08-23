using System;
using System.Threading.Tasks;
using CardOverflow.Api;
using CardOverflow.Entity;
using CardOverflow.Pure;
using Microsoft.AspNetCore.Mvc;
using ThoughtDesign.WebLibrary;

namespace CardOverflow.UserContentApi.Controllers {
  [ApiController]
  //[Route("[controller]")]
  public class Commield : Controller {
    private readonly CardOverflowDb _db;

    public Commield(CardOverflowDb db) => _db = db;

    [HttpGet("communalfield/{id}")]
    public async Task<IActionResult> Get(Guid id) => (await CommieldRepository.get(_db, id)).ToTextHtmlContent(this);

    [HttpGet("communalfieldleaf/{id}")]
    public async Task<IActionResult> GetLeaf(Guid id) => (await CommieldRepository.getLeaf(_db, id)).ToTextHtmlContent(this);

  }
}
