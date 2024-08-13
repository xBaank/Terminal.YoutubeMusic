using Microsoft.EntityFrameworkCore;

namespace Console.Database;

internal class MyDbContext(DbContextOptions<MyDbContext> options) : DbContext(options)
{
    public DbSet<LocalSong> Songs { get; set; }
    public DbSet<LocalPlaylist> Playlists { get; set; }
    public DbSet<LocalPlaylistSong> PlaylistSongs { get; set; }
    public DbSet<Setting> Settings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=data.db");
        }
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
