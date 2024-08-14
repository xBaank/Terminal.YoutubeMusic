using Console.Database;
using Microsoft.EntityFrameworkCore;

namespace Console.Extensions;

internal static class MyDbContextExtensions
{
    public static async ValueTask MigrateAOTAsync(this MyDbContext context, string filename)
    {
        using Stream stream = File.OpenRead(filename);
        using StreamReader reader = new(stream);
        var sql = reader.ReadToEnd();
        await context.Database.ExecuteSqlRawAsync(sql);
    }
}
