// https://www.youtube.com/watch?v=bQ6D4VFMxc4&list=PL8h4jt35t1wjvwFnvcB2LlYL4jLRzRmoz&index=40
// https://blazorroadshow.azurewebsites.net/blazortrainfiles/FileHandlingRemix.zip

using Dual.Web.Blazor.Shared;

using System.Net.Http.Headers;
namespace DsWebApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController(ServerGlobal global, IHubContext<ModelHub> hubContextModel) : ControllerBaseWithLogger(global.Logger)
{
    string _runtimeModelDsZipPath => global.ServerSettings.RuntimeModelDsZipPath;
    string _serviceFolder => Path.GetDirectoryName(_runtimeModelDsZipPath);

    [HttpGet("{fileName}/delete")]
    public bool DeleteLocalFile(string fileName)
    {
        // get the local filename
        fileName = Path.Combine(_serviceFolder, fileName);
        if (!System.IO.File.Exists(fileName))
        {
            _logger.Warn($"Failed try to DELETE non-existing file: {fileName}");
            return true; // not there = deleted
        }

        try
        {
            System.IO.File.Delete(fileName);
            return true;
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            return false;
        }
    }


    [HttpGet]
    public List<string> GetFiles() =>
        Directory.GetFiles(_serviceFolder, "*.*")
        .Select(Path.GetFileName).ToList()
        ;

    [HttpGet("{fileName}/get")]
    public IActionResult GetFile(string fileName)
    {
        var filePath = Path.Combine(_serviceFolder, System.Web.HttpUtility.UrlDecode(fileName));

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return File(fileStream, "application/octet-stream", Path.GetFileName(filePath));
    }
    [HttpPost]
    public bool UploadFileChunk([FromBody] FileChunk fileChunk)
    {
        try
        {
            // get the local filename
            string fileName = Path.Combine(_serviceFolder, fileChunk.Path);

            _logger.Debug($"UploadFileChunk: {fileName}");

            // delete the file if necessary
            if (fileChunk.IsFirstChunk)
                System.IO.File.Delete(fileName);

            // open for writing
            using (var stream = System.IO.File.OpenWrite(fileName))
            {
                Trace.WriteLine($"Uploading {fileName}: {fileChunk.Offset} ~+ {fileChunk.Data.Length}");
                stream.Seek(fileChunk.Offset, SeekOrigin.Begin);
                stream.Write(fileChunk.Data, 0, fileChunk.Data.Length);
            }

            if (fileChunk.IsLastChunk)
            {
                // dszip 파일 신규 upload 에 대한 처리
                System.IO.File.Move(fileName, _runtimeModelDsZipPath);
                global.ReloadRuntimeModel();
                hubContextModel.Clients.All.SendAsync(SK.S2CNModelChanged, new RuntimeModelDto(_runtimeModelDsZipPath, false));
            }

            return true;
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            return false;
        }
    }


    // https://code-maze.com/aspnetcore-webapi-add-multiple-post-actions/
    [Route("[action]")]
    [Route("AddStudent")]
    //[HttpPost("{fileName}")]
    [HttpPost]
    public bool PostFile([FromBody] byte[] data)
    {
        try
        {
            string fileName = "testfile";
            // get the local filename
            string filePath = Path.Combine(_serviceFolder, fileName);
            _logger.Debug($"PostFile: {filePath}");
            // delete the file if necessary
            System.IO.File.Delete(filePath);
            // open for writing
            using var stream = System.IO.File.OpenWrite(filePath);
            stream.Write(data, 0, data.Length);
            return true;
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            return false;
        }
    }


    // https://code-maze.com/upload-files-dot-net-core-angular/
    [HttpPost("UploadFile"), DisableRequestSizeLimit]
    public IActionResult UploadFile(IFormFile formFile)
    {
        var file = Request.Form.Files[0];
        var folderName = Path.Combine("Resources", "Images");
        if (file.Length > 0)
        {
            var fileName =
                ContentDispositionHeaderValue.Parse(file.ContentDisposition)
                    .FileName?.Trim('"')
                    !;
            var fullPath = Path.Combine(_serviceFolder, fileName);
            var dbPath = Path.Combine(folderName, fileName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                file.CopyTo(stream);
            }
            return Ok(new { dbPath });
        }
        else
        {
            return BadRequest();
        }


        //Trace.WriteLine("UploadFile" + formFile.fileName);
        //return Ok(formFile.fileName);
    }
}
