namespace DsWebApp.Shared;

public class FileChunk
{
    public string Path { get; set; } = "";
    public long Offset { get; set; }
    public byte[] Data { get; set; }
    public bool IsFirstChunk { get; set; }
    public bool IsLastChunk { get; set; }
}

