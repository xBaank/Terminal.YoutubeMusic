using System.Text.Json;
using Console.Database;
using YoutubeExplode.Videos;
using static Console.Utils;

namespace Console.LocalPlaylists;

internal static class PlaylistImporter
{
    public static IReadOnlyCollection<string> ListPlaylists() =>
        Directory
            .GetFiles(PlaylistDirectiory)
            .Select(i => Path.GetFileNameWithoutExtension(i))
            .ToList();

    public static async Task<IReadOnlyCollection<IVideo>> ImportAsync(
        string playlistName,
        CancellationToken cancellationToken = default
    )
    {
        Directory.CreateDirectory(PlaylistDirectiory);
        var path = Path.Combine(PlaylistDirectiory, playlistName);
        var jsonPath = Path.ChangeExtension(path, "json");
        using var file = File.OpenRead(jsonPath);

        return await JsonSerializer.DeserializeAsync<List<LocalSong>>(
                file,
                jsonSerializerOptions,
                cancellationToken: cancellationToken
            ) ?? throw new InvalidOperationException();
    }
}
