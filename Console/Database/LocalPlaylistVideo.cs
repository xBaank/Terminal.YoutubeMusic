using YoutubeExplode.Common;
using YoutubeExplode.Videos;

namespace Console.Database;

internal class LocalSong : IVideo
{
    public required string InternalId { get; set; }
    public required string Url { get; set; }
    public required string Title { get; set; }
    public required Author Author { get; set; }
    public required TimeSpan? Duration { get; set; }
    public required IReadOnlyList<Thumbnail> Thumbnails { get; set; }
    public ICollection<LocalPlaylistSong> PlaylistSongs { get; set; }

    VideoId IVideo.Id => new(InternalId);
}
