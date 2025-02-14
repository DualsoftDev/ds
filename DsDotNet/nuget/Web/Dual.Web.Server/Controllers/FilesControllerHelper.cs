// https://www.youtube.com/watch?v=bQ6D4VFMxc4&list=PL8h4jt35t1wjvwFnvcB2LlYL4jLRzRmoz&index=40
// https://blazorroadshow.azurewebsites.net/blazortrainfiles/FileHandlingRemix.zip

using Dual.Web.Blazor.Shared;
using log4net;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace DsWebApp.Server.Controllers;

public class FilesControllerHelper(ILog logger) //: ControllerBase
{
    public string ServiceFolder { get; set; }
    public Action<string> OnFileUploaded { get; set; }
    public bool DeleteLocalFile(string fileName)
    {
        // get the local filename
        fileName = Path.Combine(ServiceFolder, fileName);
        if (!System.IO.File.Exists(fileName))
        {
            logger.Warn($"Failed try to DELETE non-existing file: {fileName}");
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


    public List<string> GetFiles() =>
        Directory.GetFiles(ServiceFolder, "*.*")
        .Select(Path.GetFileName).ToList()
        ;

    //private IActionResult GetFileNaiveImpl(string fileName)
    //{
    //    var filePath = Path.Combine(ServiceFolder, System.Web.HttpUtility.UrlDecode(fileName));

    //    if (!System.IO.File.Exists(filePath))
    //        return NotFound();

    //    var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
    //    return File(fileStream, "application/octet-stream", Path.GetFileName(filePath));
    //}

    public FileStreamResult GetFile(string fileName)
    {
        var filePath = Path.Combine(ServiceFolder, System.Web.HttpUtility.UrlDecode(fileName));

        if (!System.IO.File.Exists(filePath))
            return null;

        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return new FileStreamResult(fileStream, "application/octet-stream")
        {
            FileDownloadName = Path.GetFileName(filePath)
        };
    }

    public bool UploadFileChunk([FromBody] FileChunk fileChunk)
    {
        try
        {
            // get the local filename
            string fileName = Path.Combine(ServiceFolder, fileChunk.Path);

            logger.Debug($"UploadFileChunk: {fileName}");

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
                // 파일 신규 upload 에 대한 처리
                OnFileUploaded?.Invoke(fileName);
            }

            return true;
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            return false;
        }
    }


    public bool PostFile([FromBody] byte[] data)
    {
        try
        {
            string fileName = "testfile";
            // get the local filename
            string filePath = Path.Combine(ServiceFolder, fileName);
            logger.Debug($"PostFile: {filePath}");
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


    public string UploadFile(IFormFile file)
    {
        var folderName = Path.Combine("Resources", "Images");
        if (file.Length > 0)
        {
            var fileName =
                ContentDispositionHeaderValue.Parse(file.ContentDisposition)
                    .FileName?.Trim('"')
                    !;
            var fullPath = Path.Combine(ServiceFolder, fileName);
            var dbPath = Path.Combine(folderName, fileName);
            using FileStream stream = new FileStream(fullPath, FileMode.Create);
            file.CopyTo(stream);
            return dbPath;
        }
        else
            return null;
    }
}
