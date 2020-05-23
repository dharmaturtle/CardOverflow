using System.Threading.Tasks;
using CardOverflow.Api;
using CardOverflow.Entity;
using CardOverflow.Pure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FSharp.Core;
using ThoughtDesign.WebLibrary;

namespace CardOverflow.UserContentApi.Controllers {
  [ApiController]
  //[Route("[controller]")]
  public class Branch : Controller {
    private readonly CardOverflowDb _db;

    public Branch(CardOverflowDb db) {
      _db = db;
    }

    [HttpGet("branch/{id}/{index}/front")]
    public async Task<IActionResult> Front(int id, int index) => _Front(index, await StackViewRepository.get(_db, id));

    [HttpGet("branch/{id}/{index}/back")]
    public async Task<IActionResult> Back(int id, int index) => _Back(index, await StackViewRepository.get(_db, id));

    [HttpGet("branchinstance/{id}/{index}/front")]
    public async Task<IActionResult> InstanceFront(int id, int index) => _Front(index, await StackViewRepository.instance(_db, id));

    [HttpGet("branchinstance/{id}/{index}/back")]
    public async Task<IActionResult> InstanceBack(int id, int index) => _Back(index, await StackViewRepository.instance(_db, id));

    private ContentResult _Front(int index, FSharpResult<BranchInstanceView, string> view) =>
      ( view.IsError
      ? view.ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynthIndex(index).IsError
      ? view.ResultValue.FrontBackFrontSynthBackSynthIndex(index).ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynthIndex(index).ResultValue.Item1).ToTextHtmlContent(this);

    private ContentResult _Back(int index, FSharpResult<BranchInstanceView, string> view) =>
      ( view.IsError
      ? view.ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynthIndex(index).IsError
      ? view.ResultValue.FrontBackFrontSynthBackSynthIndex(index).ErrorValue
      : view.ResultValue.FrontBackFrontSynthBackSynthIndex(index).ResultValue.Item2).ToTextHtmlContent(this);

  }
}
