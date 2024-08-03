using System.Globalization;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Console.Audio;
using Console.Cookies;
using Console.Database;
using Console.Extensions;
using Console.Views;
using Microsoft.Extensions.DependencyInjection;
using Terminal.Gui;
using YoutubeExplode;

namespace Console.Commands;

[Command]
internal class MainCommand : ICommand
{
    [CommandOption("cookies-path", Description = "Youtube music cookies path")]
    public string? CookiesPath { get; set; } = null;

    [CommandOption("account-index", Description = "Youtube music account index")]
    public int? AccountIndex { get; set; } = null;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        Utils.ConfigurePlatformDependencies();

        Application.Init();

        var top = new Toplevel();

        var customColors = new ColorScheme
        {
            Normal = new Terminal.Gui.Attribute(Color.Parse("#FFFFFF"), Color.Parse("#1C1C1C")), // White on Dark Gray
            HotNormal = new Terminal.Gui.Attribute(Color.Parse("#FFD700"), Color.Parse("#1C1C1C")), // Gold on Dark Gray
            Focus = new Terminal.Gui.Attribute(Color.Parse("#FF4500"), Color.Parse("#1C1C1C")), // OrangeRed on Dark Gray
            HotFocus = new Terminal.Gui.Attribute(Color.Parse("#FF6347"), Color.Parse("#1C1C1C")) // Tomato on Dark Gray
        };

        Colors.ColorSchemes["Menu"] = customColors;

        var queueWin = new Window
        {
            Title = "Playlist",
            X = 0,
            BorderStyle = LineStyle.Rounded,
            Y = 1,
            Width = Dim.Percent(20),
            Height = Dim.Fill(),
            ColorScheme = customColors
        };

        var searchWin = new Window
        {
            Title = "Search",
            X = Pos.Right(queueWin),
            BorderStyle = LineStyle.Rounded,
            Y = 0,
            Width = Dim.Fill(),
            Height = 3,
            ColorScheme = customColors
        };

        var videosWin = new View
        {
            X = Pos.Right(queueWin),
            Y = Pos.Bottom(searchWin),
            Width = Dim.Fill(),
            Height = Dim.Fill() - 8,
            ColorScheme = customColors
        };

        var playerWin = new Window
        {
            Title = "Player",
            X = Pos.Right(queueWin),
            BorderStyle = LineStyle.Rounded,
            Y = Pos.AnchorEnd(8),
            Height = 7,
            ColorScheme = customColors
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

        resultsTab.View = new View().WithPos(0).WithFill();
        recommendationsTab.View = new View().WithPos(0).WithFill();
        localPlaylistsTab.View = new View().WithPos(0).WithFill();

        tabView.AddTab(recommendationsTab, true);
        tabView.AddTab(resultsTab, false);
        tabView.AddTab(localPlaylistsTab, false);

        videosWin.Add(tabView);

        var serviceProvider = new ServiceCollection()
            .AddScoped<MyDbContext>()
            .AddSingleton<SharedCancellationTokenSource>()
            .AddSingleton<HttpClient>()
            .AddSingleton(provider =>
            {
                var youtubeClient = provider.GetRequiredService<YoutubeClient>();
                return new PlayerController(youtubeClient);
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
                return new QueueView(queueWin, playerController);
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
                    sharedCancellationTokenSource
                );
            })
            .BuildServiceProvider();

        // Resolve and use services
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

        var statusBar = new StatusBar(
            [
                new Shortcut(Key.Esc, "Exit", () => { }),
                new Shortcut(Key.Q.WithCtrl, "Search", searchWin.SetFocus),
                new Shortcut(
                    Key.L.WithCtrl,
                    "Rotate tabs",
                    () =>
                    {
                        var current = tabView.Tabs.ToList().IndexOf(tabView.SelectedTab);
                        if (current == tabView.Tabs.Count - 1)
                        {
                            tabView.SelectedTab = tabView.Tabs.ElementAt(0);
                        }
                        else
                        {
                            tabView.SelectedTab = tabView.Tabs.ElementAt(current + 1);
                        }
                        tabView.SetFocus();
                        tabView.EnsureSelectedTabIsVisible();
                    }
                ),
                new Shortcut(Key.P.WithCtrl, "Player", playerWin.SetFocus),
                new Shortcut(Key.M.WithCtrl, "Playlist", queueWin.SetFocus),
                new Shortcut(
                    Key.P.WithAlt,
                    "Save Playlist",
                    async () => await queueView.SavePlaylist()
                ),
                new Shortcut(
                    Key.Space.WithCtrl,
                    "Seek",
                    async () =>
                    {
                        var result = Utils.ShowInputDialog(
                            "Seek time",
                            "Enter seek time with the format : HH:MM:SS",
                            customColors
                        );

                        if (result is null)
                        {
                            return;
                        }

                        var isParsed = TimeSpan.TryParseExact(
                            result,
                            "g",
                            CultureInfo.InvariantCulture,
                            out var time
                        );

                        if (isParsed)
                        {
                            await playerController.SeekAsync(time);
                        }
                    }
                ),
            ]
        );

        top.Add(queueWin, searchWin, videosWin, playerWin, statusBar);

        Application.Run(top);
        top.Dispose();
        Application.Shutdown();
    }
}
