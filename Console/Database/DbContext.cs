using Microsoft.EntityFrameworkCore;
using YoutubeExplode.Playlists;
using static Terminal.Gui.SpinnerStyle;

namespace Console.Database;

internal class MyDbContext : DbContext
{
    public DbSet<LocalSong> Songs { get; set; }
    public DbSet<LocalPlaylist> Playlists { get; set; }
    public DbSet<LocalPlaylistSong> PlaylistSongs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=my_playlists.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LocalPlaylistSong>().HasKey(ps => new { ps.PlaylistId, ps.SongId });

        modelBuilder
            .Entity<LocalPlaylistSong>()
            .HasOne(ps => ps.Playlist)
            .WithMany(p => p.PlaylistSongs)
            .HasForeignKey(ps => ps.PlaylistId);

        modelBuilder
            .Entity<LocalPlaylistSong>()
            .HasOne(ps => ps.Song)
            .WithMany(s => s.PlaylistSongs)
            .HasForeignKey(ps => ps.SongId);
    }
}
