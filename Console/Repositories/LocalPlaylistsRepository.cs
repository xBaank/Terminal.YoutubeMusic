using System.Data;
using Console.Database;
using Dapper;
using YoutubeExplode.Videos;

namespace Console.Repositories;

internal class LocalPlaylistsRepository(IDbConnection db) : IDisposable
{
    public void Dispose() => db.Dispose();

    public async ValueTask<IReadOnlyCollection<LocalPlaylist>> GetPlaylistsAsync()
    {
        const string playlistsSql = "SELECT * FROM Playlists;";
        const string playlistSongsSql =
            @"
        SELECT * 
        FROM PlaylistSongs
        WHERE PlaylistId IN (SELECT PlaylistId FROM Playlists);";

        // Execute the queries
        var playlists = (await db.QueryAsync<LocalPlaylist>(playlistsSql)).ToList();
        var playlistSongs = await db.QueryAsync<LocalPlaylistSong>(playlistSongsSql);

        // Map PlaylistSongs to Playlists
        foreach (var playlist in playlists)
        {
            playlist.PlaylistSongs = playlistSongs
                .Where(ps => ps.PlaylistId == playlist.PlaylistId)
                .ToList();
        }

        return playlists;
    }

    public async ValueTask<LocalPlaylist?> GetPlaylist(int id)
    {
        const string playlistSql = "SELECT * FROM Playlists WHERE PlaylistId = @Id;";
        const string playlistSongsSql = "SELECT * FROM PlaylistSongs WHERE PlaylistId = @Id;";
        const string songsSql =
            @"
        SELECT * 
        FROM Songs
        WHERE Id IN (SELECT SongId FROM PlaylistSongs WHERE PlaylistId = @Id);";

        // Execute the queries
        var playlist = (
            await db.QueryAsync<LocalPlaylist>(playlistSql, new { Id = id })
        ).FirstOrDefault();
        var playlistSongs = await db.QueryAsync<LocalPlaylistSong>(
            playlistSongsSql,
            new { Id = id }
        );

        var songs = await db.QueryAsync<LocalSong>(songsSql, new { Id = id });

        if (playlist is not null)
        {
            playlist.PlaylistSongs = playlistSongs
                .Where(ps => ps.PlaylistId == playlist.PlaylistId)
                .ToList();

            foreach (var item in playlist.PlaylistSongs)
            {
                item.Song = songs.FirstOrDefault(i => i.Id == item.SongId);
                item.Playlist = playlist;
            }
        }

        return playlist;
    }

    public async ValueTask SavePlaylist(string name, IEnumerable<IVideo> videos)
    {
        var playlistVideos = videos
            .Select(i => new LocalSong
            {
                ChannelTitle = i.Author.ChannelTitle,
                ChannelId = i.Author.ChannelId,
                DurationMiliseconds = i.Duration.GetValueOrDefault().TotalMilliseconds,
                Title = i.Title,
                Url = i.Url,
                Id = i.Id,
            })
            .ToList();

        var songInsertSql =
            @"
            INSERT INTO Songs (Id, Title, ChannelTitle, ChannelId, DurationMiliseconds, Url)
            VALUES (@Id, @Title, @ChannelTitle, @ChannelId, @DurationMiliseconds, @Url)
            ON CONFLICT (Id) DO UPDATE
            SET Title = excluded.Title, ChannelTitle = excluded.ChannelTitle, 
                ChannelId = excluded.ChannelId, DurationMiliseconds = excluded.DurationMiliseconds, Url = excluded.Url;";

        foreach (var song in playlistVideos)
        {
            await db.ExecuteAsync(songInsertSql, song);
        }

        var playlistInsertSql =
            @"
            INSERT INTO Playlists (Name)
            VALUES (@Name)
            RETURNING PlaylistId;";

        var playlistId = await db.QuerySingleAsync<int>(playlistInsertSql, new { Name = name });

        var playlistSongsInsertSql =
            @"
            INSERT INTO PlaylistSongs (PlaylistId, SongId, [Order])
            VALUES (@PlaylistId, @SongId, @Order);";

        var playlistSongs = playlistVideos.Select(
            (song, index) =>
                new
                {
                    PlaylistId = playlistId,
                    SongId = song.Id,
                    Order = index
                }
        );

        await db.ExecuteAsync(playlistSongsInsertSql, playlistSongs);
    }
}