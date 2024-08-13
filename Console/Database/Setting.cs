using Microsoft.EntityFrameworkCore;

namespace Console.Database;

[PrimaryKey("Id")]
internal class Setting
{
    public int Id { get; set; }
    public int Volume { get; set; }
}
