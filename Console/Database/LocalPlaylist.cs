using Dapper.Contrib.Extensions;

namespace Console.Database;

internal class LocalPlaylist
{
    [Key]
    public int PlaylistId { get; set; }
    public required string Name { get; set; }

    // Navigation property for the many-to-many relationship
    [Computed]
    public ICollection<LocalPlaylistSong> PlaylistSongs { get; set; } = [];

    public override string ToString() => Name ?? "";
}
