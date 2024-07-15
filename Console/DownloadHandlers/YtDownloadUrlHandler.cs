using Console.Extensions;
using DiscordBot.MusicPlayer.DownloadHandlers;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace Console.DownloadHandlers;

public class YtDownloadUrlHandler : IDownloadUrlHandler
{
    private const int CacheTimeInMinutes = 20;
    private readonly VideoId _id;
    private readonly YoutubeClient _youtubeClient;
    private DateTime _lastUpdate;
    private DateTime _nextUpdate;
    private Task<StreamManifest>? _value;

    public YtDownloadUrlHandler(YoutubeClient youtubeClient, VideoId id)
    {
        _youtubeClient = youtubeClient;
        _id = id;
    }

    public Task<string> GetUrl()
    {
        LoadCache();
        return _value!.GetAudioManifestAsync().GetUrl();
    }

    public async Task<int> GetSize()
    {
        LoadCache();
        return (int)(await _value!.GetAudioManifestAsync())!.Size.Bytes;
    }

    private void LoadCache()
    {
        if (_value is not null && _nextUpdate <= DateTime.Now)
        {
            _lastUpdate = DateTime.Now;
            _nextUpdate = _lastUpdate.AddMinutes(CacheTimeInMinutes);
            _value = GetDownloadUrlForVideo(_id).AsTask();
        }

        if (_value is null)
        {
            _lastUpdate = DateTime.Now;
            _nextUpdate = _lastUpdate.AddMinutes(CacheTimeInMinutes);
            _value = GetDownloadUrlForVideo(_id).AsTask();
        }
    }

    private ValueTask<StreamManifest> GetDownloadUrlForVideo(VideoId id) =>
        _youtubeClient.Videos.Streams.GetManifestAsync(id);
}
