using YoutubeExplode.Videos;

namespace Console.Database;

internal class LocalPlaylistSong
{
    public required int PlaylistId { get; set; }
    public required LocalPlaylist Playlist { get; set; }

    public required string SongId { get; set; }
    public required LocalSong Song { get; set; }
}
