using NAudio.Wave;
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
        var songLabel = new Label("No song") { X = 1, Y = 1 };

        var playPauseButton = new Button("play") { X = Pos.Center(), Y = 1, };
        var nextButton = new Button("next") { X = Pos.Right(playPauseButton), Y = 1, };
        var volumeLabel = new Label("volume:") { X = Pos.AnchorEnd(20), Y = 1 };

        player.Playing += (Video video) =>
        {
            songLabel.Text = $" Playing: {video.Title ?? "No song"}";
        };

        win.Add(songLabel, playPauseButton, nextButton, volumeLabel);
    }
}
