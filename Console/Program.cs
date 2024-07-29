using System.Globalization;
using System.Reflection.Metadata.Ecma335;
using AngleSharp.Common;
using Console;
using Console.Audio;
using Console.Views;
using Terminal.Gui;

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

var tabView = new TabView
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var resultsTab = new Tab
{
    DisplayText = "Results",
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};
var recommendationsTab = new Tab
{
    DisplayText = "Recommendations",
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

resultsTab.View = new View
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

recommendationsTab.View = new View
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

tabView.AddTab(recommendationsTab, true);
tabView.AddTab(resultsTab, false);

videosWin.Add(tabView);

await using var playerController = new PlayerController();

//TODO Add player shocuts here
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
                if (current >= tabView.Tabs.Count - 1)
                {
                    tabView.SelectedTab = tabView.Tabs.ElementAt(0);
                }
                else
                {
                    tabView.SelectedTab = tabView.Tabs.ElementAt(tabView.TabIndex + 1);
                }
                tabView.SetFocus();
                tabView.EnsureSelectedTabIsVisible();
            }
        ),
        new Shortcut(Key.P.WithCtrl, "Player", playerWin.SetFocus),
        new Shortcut(Key.M.WithCtrl, "Playlist", queueWin.SetFocus),
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

var player = new PlayerView(playerWin, playerController);
var videosResults = new VideosResultsView(resultsTab, tabView, playerController);
var recomendationsView = new RecommendationsView(recommendationsTab.View, playerController);
var videoSearch = new VideoSearchView(searchWin, videosResults, playerController);
var queue = new QueueView(queueWin, playerController);
videoSearch.ShowSearch();
player.ShowPlayer();
queue.ShowQueue();
recomendationsView.ShowRecommendations();

Application.Run(top);
top.Dispose();
Application.Shutdown();
