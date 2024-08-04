using Console.Database;
using Microsoft.EntityFrameworkCore;
using YoutubeExplode.Videos;

namespace Console.Repositories;

internal class LocalPlaylistsRepository(MyDbContext db) : IDisposable
{
    public void Dispose() => db.Dispose();

    public async ValueTask<IReadOnlyCollection<LocalPlaylist>> GetPlaylistsAsync() =>
        await db.Playlists.Include(i => i.PlaylistSongs).ToListAsync();

    public async ValueTask<LocalPlaylist?> GetPlaylist(int id) =>
        await db
            .Playlists.Include(i => i.PlaylistSongs)
            .ThenInclude(i => i.Song)
            .FirstOrDefaultAsync(i => i.PlaylistId == id);

    public async ValueTask SavePlaylist(string name, IEnumerable<IVideo> videos)
    {
        var playlistVideos = videos
            .Select(i => new LocalSong
            {
                ChannelTitle = i.Author.ChannelTitle,
                ChannelId = i.Author.ChannelId,
                Duration = i.Duration,
                Title = i.Title,
                Url = i.Url,
                Id = i.Id,
            })
            .ToList();

        foreach (var item in playlistVideos)
        {
            var alreadySong = await db.Songs.FirstOrDefaultAsync(i => i.Id == item.Id);

            if (alreadySong is null)
            {
                await db.Songs.AddAsync(item);
            }
            else
            {
                alreadySong.Title = item.Title;
                db.Songs.Update(alreadySong);
            }
        }

        var localPlaylist = new LocalPlaylist
        {
            Name = name,
            PlaylistSongs = playlistVideos
                .Select((i, index) => new LocalPlaylistSong { SongId = i.Id, Order = index })
                .ToList(),
        };
        await db.Playlists.AddAsync(localPlaylist);
        await db.SaveChangesAsync();
    }
}
