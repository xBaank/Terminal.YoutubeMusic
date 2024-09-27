using System.Data;
using Console.Database;
using Dapper;
using Dapper.Contrib.Extensions;

namespace Console.Repositories;

internal class SettingsRepository(IDbConnection db) : IDisposable
{
    public void Dispose() => db.Dispose();

    private Setting? _currentSettings;

    public async ValueTask InitializeAsync()
    {
        const string checkSettingsSql = "SELECT COUNT(*) FROM Settings;";
        const string insertDefaultSettingsSql = "INSERT INTO Settings (Volume) VALUES (50);";

        var count = await db.ExecuteScalarAsync<int>(checkSettingsSql);
        if (count == 0)
        {
            await db.ExecuteAsync(insertDefaultSettingsSql);
        }
    }

    public async ValueTask SaveSettingsAsync(
        Setting settings,
        CancellationToken cancellationToken = default
    )
    {
        await db.UpdateAsync(settings);
    }

    public async ValueTask<Setting> GetSettingsAsync(CancellationToken cancellation = default)
    {
        const string selectSettingsSql = "SELECT * FROM Settings;";

        _currentSettings ??=
            await db.QuerySingleOrDefaultAsync<Setting>(selectSettingsSql)
            ?? throw new Exception("Settings not initialized");

        return _currentSettings;
    }

    public Setting GetSettings() => GetSettingsAsync().AsTask().GetAwaiter().GetResult();
}