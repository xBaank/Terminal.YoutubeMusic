using YoutubeExplode.Videos;

namespace Console.Database;

internal class LocalPlaylistSong
{
    public int PlaylistId { get; set; }
    public required LocalPlaylist Playlist { get; set; }

    public VideoId SongId { get; set; }
    public required LocalSong Song { get; set; }
}
