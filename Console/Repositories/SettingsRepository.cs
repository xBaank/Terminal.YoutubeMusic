using Console.Database;
using Microsoft.EntityFrameworkCore;

namespace Console.Repositories;

internal class SettingsRepository(MyDbContext db) : IDisposable, IAsyncDisposable
{
    public void Dispose() => db.Dispose();

    public ValueTask DisposeAsync() => db.DisposeAsync();

    private Setting? _currentSettings;

    public async ValueTask InitializeAsync()
    {
        if (!await db.Settings.AnyAsync())
        {
            await db.Settings.AddAsync(new Setting { Volume = 50 });
            await db.SaveChangesAsync();
        }
    }

    public async ValueTask SaveSettingsAsync(
        Setting settings,
        CancellationToken cancellationToken = default
    )
    {
        db.Settings.Update(settings);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask<Setting> GetSettingsAsync(CancellationToken cancellation = default)
    {
        _currentSettings ??=
            await db.Settings.FirstOrDefaultAsync(cancellationToken: cancellation)
            ?? throw new Exception("Settings not initialized");

        return _currentSettings;
    }

    public Setting GetSettings() => GetSettingsAsync().AsTask().GetAwaiter().GetResult();
}
