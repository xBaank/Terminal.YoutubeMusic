using System.Diagnostics.CodeAnalysis;
using CliFx;
using Console.Commands;
using Dapper;

[module: DapperAot]

namespace Console;

public static class Program
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MainCommand))]
    [RequiresUnreferencedCode("Calls Terminal.Gui.Application.Init(ConsoleDriver, String)")]
    [RequiresDynamicCode("Calls Terminal.Gui.Application.Init(ConsoleDriver, String)")]
    public static async Task<int> Main() =>
        await new CliApplicationBuilder()
            .SetExecutableName("Terminal.YoutubeMusic")
            .AddCommand<MainCommand>()
            .Build()
            .RunAsync();
}
