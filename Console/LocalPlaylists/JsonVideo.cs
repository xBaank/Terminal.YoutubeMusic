using YoutubeExplode.Common;
using YoutubeExplode.Videos;

namespace Console.LocalPlaylists;

internal record JsonVideo(
    VideoId Id,
    string Url,
    string Title,
    Author Author,
    TimeSpan? Duration,
    IReadOnlyList<Thumbnail> Thumbnails
) : IVideo { }
