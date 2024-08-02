using System.Text.Json;
using YoutubeExplode.Videos;
using static Console.Utils;

namespace Console.LocalPlaylists;

internal static class PlaylistExporter
{
    public static async Task ExportAsync(
        string playlistName,
        IReadOnlyCollection<IVideo> videos,
        CancellationToken cancellationToken = default
    )
    {
        Directory.CreateDirectory(PlaylistDirectiory);
        var path = Path.Combine(PlaylistDirectiory, playlistName);
        var jsonPath = Path.ChangeExtension(path, "json");
        using var file = File.Create(jsonPath);
        await JsonSerializer.SerializeAsync(
            file,
            videos,
            jsonSerializerOptions,
            cancellationToken: cancellationToken
        );
    }
}
