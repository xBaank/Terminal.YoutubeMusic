namespace Console.Audio.DownloadHandlers;

public interface IDownloadUrlHandler
{
    Task<string> GetUrl();
    Task<long> GetSize();
}
