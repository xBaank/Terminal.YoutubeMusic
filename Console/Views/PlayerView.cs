using Terminal.Gui;
using YoutubeExplode.Videos;

namespace Console.Views;

public class PlayerView(Window win, PlayerController player)
{
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    public void ShowPlayer()
    {
        win.RemoveAll();

        var playPauseButton = new Button("play") { X = 0, Y = 1 };
        var nextButton = new Button("next") { X = Pos.Right(playPauseButton) + 2, Y = 1 };

        var controlContainer = new View()
        {
            X = Pos.Center(),
            Y = Pos.AnchorEnd(3),
            Width = 20,
            Height = 5,
            CanFocus = false
        };
        controlContainer.Add(playPauseButton, nextButton);

        win.Title = $"Playing nothing | 00:00/00:00 | Volume {player.Volume}%";

        player.Playing += (Video video) =>
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            win.Title =
                $"Playing: {video.Title ?? "No song"} | Time: 00:00/00:00  | Volume {player.Volume}%";

            Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    win.Title =
                        $"Playing: {video.Title ?? "No song"} | {player.Time?.ToString(@"hh\:mm\:ss")}/{player.TotalTime?.ToString(@"hh\:mm\:ss")} | Volume {player.Volume}%";

                    await Task.Delay(1000);
                }
            });
        };

        win.Add(controlContainer);
    }
}
