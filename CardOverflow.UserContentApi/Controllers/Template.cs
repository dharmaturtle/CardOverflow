using System.Threading.Tasks;
using CardOverflow.Api;
using CardOverflow.Entity;
using CardOverflow.Pure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FSharp.Core;

namespace CardOverflow.UserContentApi.Controllers {
  [ApiController]
  //[Route("[controller]")]
  public class Template : Controller {
    private readonly CardOverflowDb _db;

    public Template(CardOverflowDb db) => _db = db;

    [HttpGet("template/{id}/front")]
    public async Task<IActionResult> Front(int id) => _front(await TemplateRepository.latest(_db, id));

    [HttpGet("template/{id}/back")]
    public async Task<IActionResult> Back(int id) => _back(await TemplateRepository.latest(_db, id));

    [HttpGet("templateinstance/{id}/front")]
    public async Task<IActionResult> InstanceFront(int id) => _front(await TemplateRepository.instance(_db, id));

    [HttpGet("templateinstance/{id}/back")]
    public async Task<IActionResult> InstanceBack(int id) => _back(await TemplateRepository.instance(_db, id));

    private ContentResult _front(FSharpResult<TemplateInstance, string> view) =>
      (view.IsError ? view.ErrorValue : view.ResultValue.FrontBackFrontSynthBackSynth.Item1).ToTextHtmlContent(this);

    private ContentResult _back(FSharpResult<TemplateInstance, string> view) =>
      (view.IsError ? view.ErrorValue : view.ResultValue.FrontBackFrontSynthBackSynth.Item2).ToTextHtmlContent(this);

  }
}
