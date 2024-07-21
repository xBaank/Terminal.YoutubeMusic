﻿using System.Globalization;
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

var videosWin = new Window
{
    Title = "Videos",
    X = Pos.Right(queueWin),
    BorderStyle = LineStyle.Rounded,
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

await using var playerController = new PlayerController();

//TODO Add player shocuts here
var statusBar = new StatusBar(
    [
        new Shortcut(Key.Esc, "Exit", () => { }),
        new Shortcut(Key.Q.WithCtrl, "Search", searchWin.SetFocus),
        new Shortcut(Key.L.WithCtrl, "Videos", videosWin.SetFocus),
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
var videosResults = new VideosResultsView(videosWin, playerController);
var videoSearch = new VideoSearchView(searchWin, videosResults, playerController);
var queue = new QueueView(queueWin, playerController);
videoSearch.ShowSearch();
player.ShowPlayer();
queue.ShowQueue();

Application.Run(top);
top.Dispose();
Application.Shutdown();
