using System.Diagnostics.CodeAnalysis;
using CliFx;
using Console.Commands;
using Dapper;

[module: DapperAot]

namespace Console;

public static class Program
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MainCommand))]
    public static async Task<int> Main() =>
        await new CliApplicationBuilder().AddCommand<MainCommand>().Build().RunAsync();
}
