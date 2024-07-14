using Console.Audio;
using Console.Views;
using Terminal.Gui;

Application.Init();

var top = new Toplevel();

var customColors = new ColorScheme
{
    Normal = new Terminal.Gui.Attribute(Color.Gray, Color.Black),
    Focus = new Terminal.Gui.Attribute(Color.Red, Color.Black),
    HotFocus = new Terminal.Gui.Attribute(Color.Red, Color.Black),
};

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
    Y = 1,
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
    Height = Dim.Fill() - 5,
    ColorScheme = customColors
};

var playerWin = new Window
{
    Title = "Player",
    X = Pos.Right(queueWin),
    Y = Pos.AnchorEnd(5),
    Width = Dim.Fill(),
    Height = 5,
    ColorScheme = customColors
};

top.Add(queueWin, searchWin, videosWin, playerWin);

await using var playerController = new PlayerController();

Application.Invoke(() =>
{
    var player = new PlayerView(playerWin, playerController);
    var videosResults = new VideosResultsView(videosWin, playerController);
    var videoSearch = new VideoSearchView(searchWin, videosResults, playerController);
    var queue = new QueueView(queueWin, playerController);
    videoSearch.ShowSearch();
    player.ShowPlayer();
    queue.ShowQueue();
});

Application.Run(top);
Application.Shutdown();
