using Microsoft.EntityFrameworkCore;
using YoutubeExplode.Playlists;

namespace Console.Database;

[PrimaryKey("PlaylistId")]
internal class LocalPlaylist
{
    public int PlaylistId { get; set; }
    public required string Name { get; set; }

    // Navigation property for the many-to-many relationship
    public ICollection<LocalPlaylistSong> PlaylistSongs { get; set; } = [];

    public override string ToString() => Name ?? "";
}
