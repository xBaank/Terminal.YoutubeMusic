namespace DiscordBot.MusicPlayer.DownloadHandlers;

public interface IDownloadUrlHandler
{
    Task<string> GetUrl();
}