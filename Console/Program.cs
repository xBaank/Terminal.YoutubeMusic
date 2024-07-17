﻿using Console;
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
    Title = "Queue",
    X = 0,
    Y = 1,
    Width = Dim.Percent(20),
    Height = Dim.Fill(),
    ColorScheme = customColors
};

var searchWin = new Window
{
    Title = "Search",
    X = Pos.Right(queueWin),
    Y = 0,
    Width = Dim.Fill(),
    Height = 3,
    ColorScheme = customColors
};

var videosWin = new Window
{
    Title = "Videos",
    X = Pos.Right(queueWin),
    Y = Pos.Bottom(searchWin),
    Width = Dim.Fill(),
    Height = Dim.Fill() - 6,
    ColorScheme = customColors
};

var playerWin = new Window
{
    Title = "Player",
    X = Pos.Right(queueWin),
    Y = Pos.AnchorEnd(6),
    Width = Dim.Fill(),
    Height = 5,
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
        new Shortcut(
            Key.K.WithCtrl,
            "Seek",
            () => {
                //TODO Prompt or move the user to a TextField to ask for the specific time
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
