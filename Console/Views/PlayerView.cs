using NAudio.Wave;
using Terminal.Gui;
using YoutubeExplode.Videos;

namespace Console.Views;

public class PlayerView(Window win, PlayerController player)
{
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    private void ResetTitle() =>
        win.Title = player.Song is null
            ? $"Playing nothing | Time: 00:00/00:00  | Volume {player.Volume}%"
            : $"Playing: {player.Song.Title ?? "No song"} | {player.Time?.ToString(@"hh\:mm\:ss")}/{player.TotalTime?.ToString(@"hh\:mm\:ss")} | Volume {player.Volume}%";

    public void ShowPlayer()
    {
        win.RemoveAll();
        ResetTitle();

        var playPauseButton = new Button("pause") { X = 0, Y = 1 };
        var nextButton = new Button("next") { X = Pos.Right(playPauseButton) + 2, Y = 1 };

        var volumeUpButton = new Button("+") { X = 0, Y = 1 };
        var volumeDownButton = new Button("-") { X = Pos.Right(volumeUpButton) + 2, Y = 1 };

        var controlContainer = new View()
        {
            X = Pos.Center(),
            Y = Pos.AnchorEnd(3),
            Width = 20,
            Height = 5,
            CanFocus = false
        };
        controlContainer.Add(playPauseButton, nextButton);

        var volumeContainer = new View()
        {
            X = Pos.AnchorEnd(15),
            Y = Pos.Y(controlContainer),
            Width = 20,
            Height = 5,
            CanFocus = false
        };
        volumeContainer.Add(volumeUpButton, volumeDownButton);

        volumeUpButton.Clicked += () =>
        {
            player.Volume += 5;
            ResetTitle();
        };

        volumeDownButton.Clicked += () =>
        {
            player.Volume -= 5;
            ResetTitle();
        };

        playPauseButton.Clicked += async () =>
        {
            if (player.State is null)
                return;

            if (player.State == PlaybackState.Playing)
            {
                playPauseButton.Text = "play";
                await player.Pause();
            }
            else
            {
                playPauseButton.Text = "pause";
                await player.PlayAsync();
            }
        };

        nextButton.Clicked += async () =>
        {
            _cancellationTokenSource.Cancel();
            ResetTitle();
            await player.SkipAsync();
            playPauseButton.Text = "pause";
        };

        player.Playing += (Video video) =>
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            Task.Run(
                async () =>
                {
                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        ResetTitle();
                        await Task.Delay(1000, _cancellationTokenSource.Token);
                    }
                },
                _cancellationTokenSource.Token
            );
        };

        win.Add(controlContainer, volumeContainer);
    }
}
