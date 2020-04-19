using System.Threading.Tasks;
using CardOverflow.Api;
using CardOverflow.Entity;
using CardOverflow.Pure;
using Microsoft.AspNetCore.Mvc;
using ThoughtDesign.WebLibrary;

namespace CardOverflow.UserContentApi.Controllers {
  [ApiController]
  //[Route("[controller]")]
  public class Index : Controller {
    private readonly UrlProvider _urlProvider;

    public Index(UrlProvider urlProvider) =>
      _urlProvider = urlProvider;

    [HttpGet("/")]
    public IActionResult Get() =>
      Redirect(_urlProvider.ServerSideBlazor);

  }
}
