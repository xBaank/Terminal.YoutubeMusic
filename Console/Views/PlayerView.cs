using Console.Audio;
using Console.Extensions;
using OpenTK.Audio.OpenAL;
using Terminal.Gui;

namespace Console.Views;

public class PlayerView(Window win, PlayerController player)
{
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    private void ResetTitle() =>
        win.Title = player.Song is null
            ? $"Playing nothing | Time: 00:00/00:00  | Volume {player.Volume}%"
            : $"Playing: {player.Song.Title.Sanitize() ?? "No song"} | {player.Time?.ToString(@"hh\:mm\:ss")}/{player.TotalTime?.ToString(@"hh\:mm\:ss")} | Volume {player.Volume}%";

    public void ShowPlayer()
    {
        win.RemoveAll();
        ResetTitle();

        var baseContainer = new View { Height = Dim.Auto(), Width = Dim.Auto(), };

        var backButton = new Button
        {
            Title = "<",
            X = 0,
            Y = 1
        };

        var playPauseButton = new Button
        {
            Title = "pause",
            X = Pos.Right(backButton) + 2,
            Y = 1,
            Width = Dim.Auto(),
        };

        var nextButton = new Button
        {
            Title = ">",
            X = Pos.Right(playPauseButton) + 2,
            Y = 1
        };

        var loopButton = new Button
        {
            Title = "loop OFF",
            X = Pos.Right(nextButton) + 2,
            Y = 1
        };

        var volumeDownButton = new Button
        {
            Title = "-",
            X = Pos.Right(loopButton) + 2,
            Y = 1
        };

        var volumeUpButton = new Button
        {
            Title = "+",
            X = Pos.Right(volumeDownButton) + 2,
            Y = 1
        };

        var progressBar = new ProgressBar()
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = 5,
            Visible = true,
            Fraction = 0.0f,
            ProgressBarStyle = ProgressBarStyle.Continuous,
        };

        var progressContainer = new View()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 2,
            CanFocus = false
        };

        progressContainer.Add(progressBar);

        var controlContainer = new View()
        {
            X = Pos.Center(),
            Y = Pos.Bottom(progressContainer),
            Width = Dim.Auto(),
            Height = Dim.Auto(),
            CanFocus = false
        };

        controlContainer.Add(
            backButton,
            playPauseButton,
            nextButton,
            loopButton,
            volumeDownButton,
            volumeUpButton
        );

        baseContainer.Add(progressContainer, controlContainer);

        async Task BackSong()
        {
            _cancellationTokenSource.Cancel();
            await player.GoBackAsync();
            playPauseButton.Text = "pause";
            progressBar.Fraction = 0;
            ResetTitle();
            await player.PlayAsync();
        }

        async Task NextSong(bool bypassLoop)
        {
            _cancellationTokenSource.Cancel();
            await player.SkipAsync(bypassLoop);
            playPauseButton.Text = "pause";
            progressBar.Fraction = 0;
            ResetTitle();
            await player.PlayAsync();
        }

        progressBar.MouseClick += async (obj, args) =>
        {
            // Calculate the fraction based on the click position
            var clickedX = args.MouseEvent.Position.X;
            var progressBarWidth = progressBar.Frame.Width;
            var fraction = (float)clickedX / progressBarWidth;

            // Ensure the fraction is within 0 to 1 range
            fraction = Math.Max(0, Math.Min(1, fraction));

            var timeToSeek = fraction * player.TotalTime;
            if (timeToSeek is null)
                return;
            await player.SeekAsync(timeToSeek.Value);
        };

        volumeUpButton.Accept += (_, args) =>
        {
            player.Volume += 5;
            ResetTitle();
        };

        volumeDownButton.Accept += (_, args) =>
        {
            player.Volume -= 5;
            ResetTitle();
        };

        playPauseButton.Accept += async (_, args) =>
        {
            if (player.State is null)
                return;

            if (player.State == ALSourceState.Playing)
            {
                playPauseButton.Text = "play";

                await player.PauseAsync();
            }
            else
            {
                playPauseButton.Text = "pause";
                await player.PlayAsync();
            }
        };

        loopButton.Accept += async (_, args) =>
        {
            LoopState nextState = player.LoopState switch
            {
                LoopState.OFF => LoopState.ON,
                LoopState.ON => LoopState.ALL,
                LoopState.ALL => LoopState.OFF,
                _ => throw new InvalidOperationException("Unknown loop state")
            };

            player.LoopState = nextState;

            loopButton.Text = player.LoopState switch
            {
                LoopState.OFF => "loop OFF",
                LoopState.ON => "loop ON",
                LoopState.ALL => "loop ALL",
                _ => throw new InvalidOperationException("Unknown loop state")
            };
        };

        backButton.Accept += async (_, args) => await BackSong();
        nextButton.Accept += async (_, args) => await NextSong(true);
        player.OnFinish += async () => await NextSong(false);

        player.StateChanged += () =>
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            if (player.Song is null)
            {
                progressBar.Fraction = 0;
                ResetTitle();
                return;
            }

            Task.Run(
                async () =>
                {
                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        Application.Invoke(() =>
                        {
                            ResetTitle();
                            var totalTime = Math.Floor(player.TotalTime?.TotalSeconds ?? 0);
                            var currentTime = Math.Floor(player.Time?.TotalSeconds ?? 0);
                            progressBar.Fraction = (float)(currentTime / totalTime);

                            //TODO We must set the color after initialization because hotNormal is hardcoded on terminal.gui v2
                            if (progressBar.ColorScheme.HotNormal.Foreground != Color.Red)
                            {
                                progressBar.ColorScheme = new ColorScheme
                                {
                                    Focus = new Terminal.Gui.Attribute(
                                        Color.Parse("#FF4500"),
                                        Color.White
                                    ),
                                    HotNormal = new Terminal.Gui.Attribute(
                                        Color.Parse("#FF4500"),
                                        Color.White
                                    )
                                };
                            }
                        });
                        await Task.Delay(1000, _cancellationTokenSource.Token);
                    }
                },
                _cancellationTokenSource.Token
            );
        };

        win.Add(baseContainer);
    }
}
