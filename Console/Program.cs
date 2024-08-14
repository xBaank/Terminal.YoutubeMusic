using System.Diagnostics.CodeAnalysis;
using CliFx;
using Console.Commands;
using Console.Database;
using Console.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Console;

public static class Program
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MainCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MyDbContext))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(InitialCreate))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RemoveDescription))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Order))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Settings))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Volume_as_int))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MyDbContextModelSnapshot))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Migration))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ModelSnapshot))]
    public static async Task<int> Main() =>
        await new CliApplicationBuilder().AddCommand<MainCommand>().Build().RunAsync();
}
