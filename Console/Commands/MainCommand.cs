using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Console.Audio;
using Console.Cookies;
using Console.Extensions;
using Console.Repositories;
using Console.Views;
using DbUp;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Terminal.Gui;
using YoutubeExplode;
using static Console.Utils;

namespace Console.Commands;

[Command]
internal class MainCommand : ICommand
{
    [CommandOption("cookies-path", Description = "Youtube music cookies path")]
    public string? CookiesPath { get; set; } = null;

    [CommandOption("account-index", Description = "Youtube music account index")]
    public int? AccountIndex { get; set; } = null;

    [SuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "<Pending>"
    )]
    public async ValueTask ExecuteAsync(IConsole console)
    {
        const string connectionString = "Data Source=data.db";

        Utils.ConfigurePlatformDependencies();
        await console.Output.WriteLineAsync("Ignore any warnings above this message");
        await console.Output.WriteLineAsync("[PortAudio] Initialized corretly");
        Utils.PerformMigrations(connectionString);
        await console.Output.WriteLineAsync("[Db] Initialized corretly");

        var top = new Toplevel();

        Colors.ColorSchemes["Menu"] = CustomColor;
        var searchWin = new Window
        {
            Title = "Search",
            X = 0,
            BorderStyle = LineStyle.Rounded,
            Y = 0,
            Width = Dim.Fill(),
            Height = 3,
            ColorScheme = CustomColor
        };

        var videosWin = new View
        {
            X = 0,
            Y = Pos.Bottom(searchWin),
            Width = Dim.Fill(),
            Height = Dim.Fill()! - 8,
            ColorScheme = CustomColor
        };

        var playerWin = new Window
        {
            Title = "Player",
            X = 0,
            BorderStyle = LineStyle.Rounded,
            Y = Pos.AnchorEnd(8),
            Height = 7,
            ColorScheme = CustomColor
        };

        var tabView = new TabView().WithPos(0).WithFill();

        var resultsTab = new Tab { DisplayText = "Results" }
            .WithPos(0)
            .WithFill();
        var recommendationsTab = new Tab { DisplayText = "Recommendations" }
            .WithPos(0)
            .WithFill();
        var localPlaylistsTab = new Tab { DisplayText = "Saved playlists" }
            .WithPos(0)
            .WithFill();
        var playlistTab = new Tab { DisplayText = "Playlist" }
            .WithPos(0)
            .WithFill();

        resultsTab.View = new View().WithPos(0).WithFill();
        recommendationsTab.View = new View().WithPos(0).WithFill();
        localPlaylistsTab.View = new View().WithPos(0).WithFill();
        playlistTab.View = new View().WithPos(0).WithFill();

        tabView.AddTab(recommendationsTab, true);
        tabView.AddTab(resultsTab, false);
        tabView.AddTab(playlistTab, false);
        tabView.AddTab(localPlaylistsTab, false);

        videosWin.Add(tabView);

        var serviceProvider = new ServiceCollection()
            .AddScoped<IDbConnection>(_ => new SqliteConnection(connectionString))
            .AddScoped<LocalPlaylistsRepository>()
            .AddScoped<SettingsRepository>()
            .AddSingleton<SharedCancellationTokenSource>()
            .AddSingleton<HttpClient>()
            .AddSingleton(provider =>
            {
                var youtubeClient = provider.GetRequiredService<YoutubeClient>();
                var settingsRepository = provider.GetRequiredService<SettingsRepository>();
                return new PlayerController(youtubeClient, settingsRepository);
            })
            .AddSingleton(provider =>
            {
                var httpClient = provider.GetRequiredService<HttpClient>();
                return new AccountHandler(AccountIndex) { InnerHandler = new HttpClientHandler() };
            })
            .AddSingleton(provider =>
            {
                var httpClient = provider.GetRequiredService<HttpClient>();
                return new YoutubeClient(httpClient, CookiesUtils.GetCookies(CookiesPath));
            })
            .AddSingleton(provider =>
            {
                var playerController = provider.GetRequiredService<PlayerController>();
                return new PlayerView(playerWin, playerController);
            })
            .AddSingleton(provider =>
            {
                var playerController = provider.GetRequiredService<PlayerController>();
                return new QueueView(playlistTab.View, playerController, provider);
            })
            .AddSingleton(provider =>
            {
                var playerController = provider.GetRequiredService<PlayerController>();
                var queue = provider.GetRequiredService<QueueView>();
                var sharedCancellationTokenSource =
                    provider.GetRequiredService<SharedCancellationTokenSource>();
                return new VideosResultsView(
                    resultsTab,
                    tabView,
                    playerController,
                    queue,
                    sharedCancellationTokenSource
                );
            })
            .AddSingleton(provider =>
            {
                var playerController = provider.GetRequiredService<PlayerController>();
                var queue = provider.GetRequiredService<QueueView>();
                var sharedCancellationTokenSource =
                    provider.GetRequiredService<SharedCancellationTokenSource>();
                return new RecommendationsView(
                    recommendationsTab.View,
                    playerController,
                    queue,
                    sharedCancellationTokenSource
                );
            })
            .AddSingleton(provider =>
            {
                var playerController = provider.GetRequiredService<PlayerController>();
                var videosResults = provider.GetRequiredService<VideosResultsView>();
                return new VideoSearchView(searchWin, videosResults, playerController);
            })
            .AddSingleton(provider =>
            {
                var playerController = provider.GetRequiredService<PlayerController>();
                var queue = provider.GetRequiredService<QueueView>();
                var sharedCancellationTokenSource =
                    provider.GetRequiredService<SharedCancellationTokenSource>();
                return new LocalPlaylistsView(
                    localPlaylistsTab.View,
                    queue,
                    playerController,
                    sharedCancellationTokenSource,
                    provider
                );
            })
            .AddSingleton(provider =>
            {
                var playerController = provider.GetRequiredService<PlayerController>();
                var queue = provider.GetRequiredService<QueueView>();
                return new StatusBarFactory(searchWin, playerWin, queue, tabView, playerController);
            })
            .BuildServiceProvider();

        var statusBarFactory = serviceProvider.GetRequiredService<StatusBarFactory>();
        using var settingsRepository = serviceProvider.GetRequiredService<SettingsRepository>();
        await settingsRepository.InitializeAsync();

        top.Add(searchWin, videosWin, playerWin, statusBarFactory.Create());

        Application.Init();

        await using var playerController = serviceProvider.GetRequiredService<PlayerController>();
        var playerView = serviceProvider.GetRequiredService<PlayerView>();
        var queueView = serviceProvider.GetRequiredService<QueueView>();
        var videosResultsView = serviceProvider.GetRequiredService<VideosResultsView>();
        var recommendationsView = serviceProvider.GetRequiredService<RecommendationsView>();
        var videoSearchView = serviceProvider.GetRequiredService<VideoSearchView>();
        var localPlaylistsView = serviceProvider.GetRequiredService<LocalPlaylistsView>();

        videoSearchView.ShowSearch();
        playerView.ShowPlayer();
        queueView.ShowQueue();
        recommendationsView.ShowRecommendations();
        localPlaylistsView.ShowLocalPlaylists();

        Application.Run(top);
        top.Dispose();
        Application.Shutdown();
    }
}
