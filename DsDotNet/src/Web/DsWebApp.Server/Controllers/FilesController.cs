// https://www.youtube.com/watch?v=bQ6D4VFMxc4&list=PL8h4jt35t1wjvwFnvcB2LlYL4jLRzRmoz&index=40
// https://blazorroadshow.azurewebsites.net/blazortrainfiles/FileHandlingRemix.zip

using Dual.Web.Blazor.Shared;

using RestResultString = Dual.Web.Blazor.Shared.RestResult<string>;

namespace DsWebApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBaseWithLogger
{
    ServerGlobal _global;
    IHubContext<ModelHub> _hubContextModel;
    FilesControllerHelper _helper;
    Action<string> _onFileUploaded;


    public FilesController(ServerGlobal global, IHubContext<ModelHub> hubContextModel)
        : base(global.Logger)
    {
        _global = global;
        _hubContextModel = hubContextModel;
        _onFileUploaded = new (fileName =>
        {
            var serviceFolder = global.ServerSettings.ServiceFolder;
            // dszip 파일 신규 upload 에 대한 처리  //copy->move로 변경 동작일단 잘됨
            //var targetFile = Path.Combine(serviceFolder, Path.GetFileName(fileName));
            //System.IO.File.Copy(fileName, targetFile, overwrite:true);
            //var path = Path.Combine(serviceFolder, Path.GetFileName(fileName));
            _global.ServerSettings.RuntimeModelDsZipPath = fileName;
            _global.ReloadRuntimeModel(global.ServerSettings);
            _hubContextModel.Clients.All.SendAsync(SK.S2CNModelChanged, fileName);
        });
        _helper = new FilesControllerHelper(_logger) {  ServiceFolder = _serviceFolder, OnFileUploaded = _onFileUploaded};
    }
    string _runtimeModelDsZipPath => _global.ServerSettings.RuntimeModelDsZipPath;
    string _serviceFolder => _global.ServerSettings.ServiceFolder;

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

    // api/files/{fileName}/activate
    [HttpGet("{fileName}/activate")]
    public async Task<RestResultString> MyActivate(string fileName)
    {
        _onFileUploaded(Path.Combine(_global.ServerSettings.ServiceFolder, fileName));
        return RestResultString.Ok($"OK: {fileName}");
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
