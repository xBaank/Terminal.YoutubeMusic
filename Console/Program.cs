using CliFx;
using Console.Commands;

namespace Console;

public static class Program
{
    public static async Task<int> Main() =>
        await new CliApplicationBuilder().AddCommand<MainCommand>().Build().RunAsync();
}
