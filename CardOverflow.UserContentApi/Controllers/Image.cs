using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CardOverflow.Api;
using CardOverflow.Entity;
using CardOverflow.Pure;
using Microsoft.AspNetCore.Mvc;
using NeoSmart.Utils;

namespace CardOverflow.UserContentApi.Controllers {

  [Route("[controller]")]
  public class Image : Controller {
    private readonly CardOverflowDb _db;

    public Image(CardOverflowDb db) =>
      _db = db;

    [HttpGet("{hash}")]
    public async Task<IActionResult> GetImage(string hash) {
      var x = await FileRepository.get(_db, hash);
      if (x.IsOk) {
        var imageStream = new MemoryStream(); // don't dispose https://stackoverflow.com/a/52329792
        await imageStream.WriteAsync(x.ResultValue);
        imageStream.Position = 0;
        return new FileStreamResult(imageStream, "image/jpeg"); // medTODO store the MIME
      } else {
        return NotFound();
      }
    }

  }
}
