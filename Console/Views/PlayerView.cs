﻿using Console.Audio;
using OpenTK.Audio.OpenAL;
using Terminal.Gui;

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

        var playPauseButton = new Button
        {
            Title = "pause",
            X = Pos.Center(),
            Y = 1
        };
        var nextButton = new Button
        {
            Title = "next",
            X = Pos.Right(playPauseButton) + 2,
            Y = 1
        };

        var volumeUpButton = new Button
        {
            Title = "+",
            X = 0,
            Y = 1
        };
        var volumeDownButton = new Button
        {
            Title = "-",
            X = Pos.Right(volumeUpButton) + 2,
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
            ProgressBarFormat = ProgressBarFormat.Simple,
            ProgressBarStyle = ProgressBarStyle.MarqueeContinuous,
            ColorScheme = new ColorScheme
            {
                Normal = new Terminal.Gui.Attribute(Color.Red, Color.White)
            }
        };

        var progressContainer = new View()
        {
            X = 3,
            Y = Pos.AnchorEnd(3),
            Width = Dim.Percent(30),
            Height = 5,
            CanFocus = false
        };
        progressContainer.Add(progressBar);

        var controlContainer = new View()
        {
            X = Pos.Center(),
            Y = Pos.Y(progressContainer),
            Width = Dim.Percent(30),
            Height = 5,
            CanFocus = false
        };
        controlContainer.Add(playPauseButton, nextButton);

        var volumeContainer = new View()
        {
            X = Pos.AnchorEnd(15),
            Y = Pos.Y(controlContainer),
            Width = Dim.Percent(30),
            Height = 5,
            CanFocus = false
        };
        volumeContainer.Add(volumeUpButton, volumeDownButton);

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
            await player.Seek(timeToSeek.Value);
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

                await player.Pause();
            }
            else
            {
                playPauseButton.Text = "pause";
                await player.PlayAsync();
            }
        };

        nextButton.Accept += async (_, args) =>
        {
            _cancellationTokenSource.Cancel();
            await player.SkipAsync();
            playPauseButton.Text = "pause";
            progressBar.Fraction = 0;
            ResetTitle();
            await player.PlayAsync();
        };

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
                            var totalTime = player.TotalTime?.TotalMilliseconds ?? 0;
                            var currentTime = player.Time?.TotalMilliseconds ?? 0;
                            progressBar.Fraction = (float)(currentTime / totalTime);
                        });
                        await Task.Delay(1000, _cancellationTokenSource.Token);
                    }
                },
                _cancellationTokenSource.Token
            );
        };

        win.Add(controlContainer, volumeContainer, progressContainer);
    }
}
