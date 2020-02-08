using System.Threading.Tasks;
using CardOverflow.Api;
using CardOverflow.Entity;
using CardOverflow.Pure;
using Microsoft.AspNetCore.Mvc;

namespace CardOverflow.Server {
  public class TemplateController : Controller {
    private readonly CardOverflowDb _db;

    public TemplateController(CardOverflowDb db) => _db = db;

    [HttpGet("template/{id}/front")] // highTODO move to another server
    public async Task<IActionResult> Front(int id) =>
      Content(await TemplateRepository.getFront(_db, id), "text/html");

    [HttpGet("template/{id}/back")] // highTODO move to another server
    public async Task<IActionResult> Back(int id) =>
      Content(await TemplateRepository.getBack(_db, id), "text/html");

    [HttpGet("templateinstance/{id}/front")] // highTODO move to another server
    public async Task<IActionResult> InstanceFront(int id) =>
      Content(await TemplateRepository.getFrontInstance(_db, id), "text/html");

    [HttpGet("templateinstance/{id}/back")] // highTODO move to another server
    public async Task<IActionResult> InstanceBack(int id) =>
      Content(await TemplateRepository.getBackInstance(_db, id), "text/html");

  }
}
