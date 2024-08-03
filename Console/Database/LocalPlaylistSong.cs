using YoutubeExplode.Videos;

namespace Console.Database;

internal class LocalPlaylistSong
{
    public int PlaylistId { get; set; }
    public LocalPlaylist? Playlist { get; set; }

    public string? SongId { get; set; }
    public LocalSong? Song { get; set; }
}
