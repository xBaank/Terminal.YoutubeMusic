using Dapper.Contrib.Extensions;
using Lazy;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;

namespace Console.Database;

internal class LocalSong : IVideo
{
    [Key]
    public required string Id { get; set; }
    public required string Url { get; set; }
    public required string Title { get; set; }
    public required string ChannelId { get; set; }
    public required string ChannelTitle { get; set; }
    public required double DurationMiliseconds { get; set; }

    [Computed]
    [Lazy]
    public TimeSpan? Duration => TimeSpan.FromMilliseconds(DurationMiliseconds);

    [Computed]
    //Not saved for now, only to satisfy IVideo
    public IReadOnlyList<Thumbnail> Thumbnails { get; } = [];

    [Computed]
    public ICollection<LocalPlaylistSong>? PlaylistSongs { get; set; }

    [Computed]
    [Lazy]
    VideoId IVideo.Id => new(Id);

    [Computed]
    [Lazy]
    Author IVideo.Author => new(ChannelId, ChannelTitle);
}
