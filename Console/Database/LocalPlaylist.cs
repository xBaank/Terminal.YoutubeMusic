namespace Console.Database;

internal class LocalPlaylist
{
    public int PlaylistId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }

    // Navigation property for the many-to-many relationship
    public ICollection<LocalPlaylistSong>? PlaylistSongs { get; set; }
}
