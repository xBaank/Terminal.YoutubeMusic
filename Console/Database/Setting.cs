using Dapper.Contrib.Extensions;

namespace Console.Database;

internal class Setting
{
    [Key]
    public int Id { get; set; }
    public int Volume { get; set; }
}
