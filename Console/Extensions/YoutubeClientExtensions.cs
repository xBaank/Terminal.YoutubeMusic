using YoutubeExplode.Videos.Streams;

namespace Console.Extensions;

public static class YoutubeClientExtensions
{
    public static Task<IStreamInfo?> GetAudioManifestAsync(
        this ValueTask<StreamManifest> streamManifestTask
    ) => streamManifestTask.AsTask().GetAudioManifestAsync();

    public static async Task<IStreamInfo?> GetAudioManifestAsync(
        this Task<StreamManifest> streamManifestTask
    )
    {
        var streamManifest = await streamManifestTask;
        return streamManifest
            .GetAudioStreams()
            .Where(i => i.AudioCodec == "opus")
            .MaxBy(i => i.Bitrate);
    }

    public static async Task<string> GetUrl(this Task<IStreamInfo?> streamInfoTask)
    {
        var streamInfo = await streamInfoTask;
        return streamInfo?.Url ?? throw new Exception("No stream info found");
    }
}
