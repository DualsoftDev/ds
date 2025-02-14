using System.Net.Http.Json;
//using Newtonsoft.Json;
using System.Text.Json;
using System.Diagnostics;

namespace Dual.Web.Blazor.Shared;

public class FilesManager(HttpClient http)
{
    public async Task<List<string>> GetFileNames()
    {
        try
        {
            var result = await http.GetAsync("api/files");
            result.EnsureSuccessStatusCode();
            string responseBody = await result.Content.ReadAsStringAsync();
            return NewtonsoftJson.DeserializeObject<List<string>>(responseBody);
        }
        catch (Exception )
        {
            return new List<string>();
        }
    }

    public async Task<List<string>> GetBlobUrls(string containerName)
    {
        try
        {
            var result = await http.GetAsync($"api/files/{containerName}/blobs");
            result.EnsureSuccessStatusCode();
            string responseBody = await result.Content.ReadAsStringAsync();
            return NewtonsoftJson.DeserializeObject<List<string>>(responseBody);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<bool> DeleteFileOnServer(string filePath)
    {
        try
        {
            var result = await http.GetAsync($"api/files/{filePath}/delete");
            result.EnsureSuccessStatusCode();
            string responseBody = await result.Content.ReadAsStringAsync();
            return Convert.ToBoolean(responseBody);
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            return false;
        }
    }

    public async Task<string> CopyFileToContainer(string containerName, string filePath)
    {
        try
        {
            var result = await http.GetAsync($"api/files/{filePath}/{containerName}/copy");
            result.EnsureSuccessStatusCode();
            return await result.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            return "";
        }
    }

    public async Task<bool> UploadFileChunk(FileChunk fileChunk)
    {
        try
        {
            var result = await http.PostAsJsonAsync("api/files", fileChunk);
            result.EnsureSuccessStatusCode();
            string responseBody = await result.Content.ReadAsStringAsync();
            return Convert.ToBoolean(responseBody);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /*
     * - HttpClient 의 BaseURL 에 URL 이 명시 되어야 한다.
     *      . e.g : HttpClient client = new HttpClient() { BaseAddress = new Uri("_http://localhost:5228/") };
     * - FilesManager 의 routing 정보를 이용한다.  (api/files)
     * - Server 에서 정의된 ServiceFolder 위치에 파일을 drop 한다.
     */
    [Obsolete("사용 중인가??  vs onFileInputFileChange() @ PageUploadModel.razor")]
    /// <summary>
    /// Console application 을 이용하여 Web Server 의 Post Rest-API 를 이용해서 file 을 upload 하기 위한 용도.
    /// </summary>
    public static async Task<Exception> UploadFileAsync(HttpClient client, string filePath)
    {
        try
        {
            long uploadedBytes = 0;
            var filesManager = new FilesManager(client);


            // calculate the chunks we have to send
            long totalBytes = new FileInfo(filePath).Length;
            long chunkSize = 400000;
            long numChunks = totalBytes / chunkSize;
            long remainder = totalBytes % chunkSize;

            bool firstChunk = true;

            // Open the input and output file streams
            //using (var inStream = args.File.OpenReadStream(long.MaxValue))
            using (var inStream = File.OpenRead(filePath))
            {
                while (uploadedBytes < totalBytes)
                {
                    var whatsLeft = totalBytes - uploadedBytes;
                    if (whatsLeft < chunkSize)
                        chunkSize = remainder;
                    // Read the next chunk
                    var bytes = new byte[chunkSize];
                    var buffer = new Memory<byte>(bytes);
                    var read = await inStream.ReadAsync(buffer);

                    // create the fileChunk object
                    var chunk = new FileChunk
                    {
                        Data = bytes,
                        Path = Path.GetFileName(filePath),
                        Offset = uploadedBytes,
                        IsFirstChunk = firstChunk
                    };

                    // upload this chunk
                    bool succeeded = await filesManager.UploadFileChunk(chunk);
                    if (!succeeded)
                        throw new Exception("ERROR");

                    firstChunk = false; // no longer the first chunk

                    // Update our progress data and UI
                    uploadedBytes += read;
                    long percent = uploadedBytes * 100 / totalBytes;
                    // Report progress with a string
                    Trace.WriteLine($"Uploading {filePath} {percent}%");
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}

