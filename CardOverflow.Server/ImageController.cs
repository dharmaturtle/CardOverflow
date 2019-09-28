using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CardOverflow.Entity;
using CardOverflow.Pure;
using Microsoft.AspNetCore.Mvc;
using NeoSmart.Utils;

namespace CardOverflow.Server {

  [Route("[controller]")]
  public class ImageController : Controller {
    private readonly CardOverflowDb _db;

    public ImageController(CardOverflowDb db) {
      _db = db;
    }

    [HttpGet("{hash}")] // medTODO move to another server
    public async Task<IActionResult> GetImage(string hash) { // medTODO is this a security hazard?
      var sha256 = UrlBase64.Decode(hash);
      var imageArray = _db.File.First(x => x.Sha256 == sha256).Data;
      var imageStream = new MemoryStream();
      await imageStream.WriteAsync(imageArray);
      imageStream.Position = 0;
      return new FileStreamResult(imageStream, "image/jpeg"); // medTODO store the MIME
    }

  }
}
