using System.Text;
using Console;
using Console.Views;
using Terminal.Gui;

var useUsc = args.Any(i => i == "-usc");

System.Console.OutputEncoding = Encoding.UTF8;
Application.UseSystemConsole = useUsc;

Application.Init();

var top = Application.Top;

var customColors = new ColorScheme()
{
    Normal = Application.Driver.MakeAttribute(Color.Red, Color.Black),
    Focus = Application.Driver.MakeAttribute(Color.White, Color.Red),
};

var searchWin = new Window("Search")
{
    X = 0,
    Y = 1,
    Width = Dim.Fill(),
    Height = Dim.Percent(10),
    ColorScheme = customColors
};

var videosWin = new Window("Videos")
{
    X = 0,
    Y = Pos.Bottom(searchWin),
    Width = Dim.Fill(),
    Height = Dim.Percent(80),
    ColorScheme = customColors
};

var playerWin = new Window("Player")
{
    X = 0,
    Y = Pos.Bottom(videosWin),
    Width = Dim.Fill(),
    Height = Dim.Percent(10),
    ColorScheme = customColors
};

top.Add(searchWin, videosWin, playerWin);

using var playerController = new PlayerController();

Application.MainLoop.Invoke(() =>
{
    var player = new Player(playerWin, playerController);
    var videosResults = new VideosResults(videosWin, playerController);
    var videoSearch = new VideoSearch(searchWin, videosResults, playerController);
    videoSearch.ShowSearch();
    player.ShowPlayer();
});

Application.Run();
Application.Shutdown();
