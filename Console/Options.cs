using Terminal.Gui;

namespace Console;

public class Options(Window win)
{
    private readonly VideoSearch _videoSearch = new(win);

    public void ShowOptions()
    {
        win.RemoveAll();

        // Create a label
        var label = new Label("Select an option:") { X = 1, Y = 1 };
        win.Add(label);

        var videoSearchButton = new Button("video search")
        {
            X = 1,
            Y = Pos.Bottom(label) + 1,
            Width = 10,
            Height = 1,
            CanFocus = true
        };

        var other = new Button("other")
        {
            X = Pos.Right(videoSearchButton) + 1,
            Y = Pos.Bottom(label) + 1,
            Width = 10,
            Height = 1,
            CanFocus = true
        };

        videoSearchButton.Clicked += () =>
            Application.MainLoop.Invoke(() => _videoSearch.ShowSearch());

        win.Add(videoSearchButton);
        win.Add(other);
    }
}
