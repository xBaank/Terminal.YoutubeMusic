using System.Text;
using Concentus;
using Console;
using Console.Views;
using Terminal.Gui;

var useUsc = args.Any(i => i == "-usc");

System.Console.OutputEncoding = Encoding.UTF8;
Application.UseSystemConsole = useUsc;

Application.Init();

Application.Driver.LeftBracket = '(';
Application.Driver.RightBracket = ')';

var top = Application.Top;

var customColors = new ColorScheme()
{
    Normal = Application.Driver.MakeAttribute(Color.Gray, Color.Black),
    Focus = Application.Driver.MakeAttribute(Color.Red, Color.Black),
    HotFocus = Application.Driver.MakeAttribute(Color.Red, Color.Black),
};

var queueWin = new Window("Queue")
{
    X = 0,
    Y = 1,
    Width = Dim.Percent(20),
    Height = Dim.Fill(),
    ColorScheme = customColors
};

var searchWin = new Window("Search")
{
    X = Pos.Right(queueWin),
    Y = 1,
    Width = Dim.Fill(),
    Height = 3,
    ColorScheme = customColors
};

var videosWin = new Window("Videos")
{
    X = Pos.Right(queueWin),
    Y = Pos.Bottom(searchWin),
    Width = Dim.Fill(),
    Height = Dim.Fill() - 5,
    ColorScheme = customColors
};

var playerWin = new Window("Player")
{
    X = Pos.Right(queueWin),
    Y = Pos.AnchorEnd(5),
    Width = Dim.Fill(),
    Height = 5,
    ColorScheme = customColors
};

top.Add(queueWin, searchWin, videosWin, playerWin);

using var playerController = new PlayerController();

Application.MainLoop.Invoke(() =>
{
    var player = new PlayerView(playerWin, playerController);
    var videosResults = new VideosResultsView(videosWin, playerController);
    var videoSearch = new VideoSearchView(searchWin, videosResults, playerController);
    var queue = new QueueView(queueWin, playerController);
    videoSearch.ShowSearch();
    player.ShowPlayer();
    queue.ShowQueue();
});

Application.Run();
Application.Shutdown();

await Task.Delay(-1);
