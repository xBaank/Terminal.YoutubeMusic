using NAudio.Wave;
using Spectre.Console;
using Terminal.Gui;
using Terminal.Gui.Graphs;
using YoutubeExplode.Videos;

namespace Console.Views;

public class Player(Window win, PlayerController player)
{
    public void ShowPlayer()
    {
        win.RemoveAll();

        // Create labels




        var playPauseButton = new Button("play") { X = 0, Y = 1, };
        var nextButton = new Button("next") { X = Pos.Right(playPauseButton) + 2, Y = 1, };

        var controlContainer = new View()
        {
            X = Pos.Center(),
            Y = Pos.AnchorEnd(3),
            Width = 20,
            Height = 5,
            CanFocus = false
        };
        controlContainer.Add(playPauseButton, nextButton);

        var songLabel = new Label("No song") { X = 1, Y = 1 };
        var volumeLabel = new Label($"volume: {player.Volume}") { X = Pos.AnchorEnd(20), Y = 1 };
        var time = new Label($"Time: 00:00/00:00") { X = Pos.Center(), Y = 1 };

        player.Playing += (Video video) =>
        {
            songLabel.Text = $"Playing: {video.Title ?? "No song"}";
        };

        win.Add(songLabel, controlContainer, volumeLabel, time);
    }
}
