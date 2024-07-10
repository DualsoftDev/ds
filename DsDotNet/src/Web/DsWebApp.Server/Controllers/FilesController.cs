// https://www.youtube.com/watch?v=bQ6D4VFMxc4&list=PL8h4jt35t1wjvwFnvcB2LlYL4jLRzRmoz&index=40
// https://blazorroadshow.azurewebsites.net/blazortrainfiles/FileHandlingRemix.zip

using Dual.Web.Blazor.Shared;

namespace DsWebApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBaseWithLogger
{
    ServerGlobal _global;
    IHubContext<ModelHub> _hubContextModel;
    FilesControllerHelper _helper;
    public FilesController(ServerGlobal global, IHubContext<ModelHub> hubContextModel)
        : base(global.Logger)
    {
        _global = global;
        _hubContextModel = hubContextModel;
        var onFileUploaded = new Action<string>(fileName =>
        {
            // dszip 파일 신규 upload 에 대한 처리  //copy->move로 변경 동작일단 잘됨
            System.IO.File.Copy(fileName, _runtimeModelDsZipPath, overwrite:true);
            _global.ServerSettings.RuntimeModelDsZipPath = fileName;
            _global.ReloadRuntimeModel(global.ServerSettings);
            _hubContextModel.Clients.All.SendAsync(SK.S2CNModelChanged, fileName);
        });
        _helper = new FilesControllerHelper(_logger) {  ServiceFolder = _serviceFolder, OnFileUploaded = onFileUploaded};
    }
    string _runtimeModelDsZipPath => _global.ServerSettings.RuntimeModelDsZipPath;
    string _serviceFolder => Path.GetDirectoryName(_runtimeModelDsZipPath);

    // api/files/{fileName}/delete
    [HttpGet("{fileName}/delete")]
    public bool MyDeleteLocalFile(string fileName) => _helper.DeleteLocalFile(fileName);



    // api/files
    [HttpGet] public List<string> MyGetFiles() => _helper.GetFiles();

    // api/files/{fileName}/get
    [HttpGet("{fileName}/get")]
    public IActionResult MyGetFile(string fileName)
    {
        FileStreamResult file = _helper.GetFile(fileName);
        return file == null ? NotFound() : file;
    }

    // api/files
    [HttpPost] public bool MyUploadFileChunk([FromBody] FileChunk fileChunk) => _helper.UploadFileChunk(fileChunk);

    // https://code-maze.com/aspnetcore-webapi-add-multiple-post-actions/
    // api/files/post
    [Route("[action]")]
    [HttpPost]
    public bool MyPostFile([FromBody] byte[] data) => _helper.PostFile(data);

    // https://code-maze.com/upload-files-dot-net-core-angular/
    [HttpPost("UploadFile"), DisableRequestSizeLimit]
    public IActionResult MyUploadFile(IFormFile formFile)
    {
        var file = Request.Form.Files[0];
        string uploadedPath = _helper.UploadFile(file);
        return uploadedPath.IsNullOrEmpty() ? BadRequest() : Ok(new { uploadedPath });

        //Debug.WriteLine("UploadFile" + formFile.fileName);
        //return Ok(formFile.fileName);
    }
}
