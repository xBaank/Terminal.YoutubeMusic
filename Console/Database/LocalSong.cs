using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;

namespace Console.Database;

[PrimaryKey("Id")]
internal class LocalSong : IVideo
{
    public required string Id { get; set; }
    public required string Url { get; set; }
    public required string Title { get; set; }
    public required string ChannelId { get; set; }
    public required string ChannelTitle { get; set; }
    public required TimeSpan? Duration { get; set; }

    [NotMapped]
    public IReadOnlyList<Thumbnail> Thumbnails { get; } = [];
    public ICollection<LocalPlaylistSong>? PlaylistSongs { get; set; }

    VideoId IVideo.Id => new(Id);
    Author IVideo.Author => new(ChannelId, ChannelTitle);
}
