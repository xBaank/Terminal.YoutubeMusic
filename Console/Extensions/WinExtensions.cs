using Terminal.Gui;

namespace Console.Extensions;

public static class WinExtensions
{
    public static View DisplayProgressBar(this Window window)
    {
        window.RemoveAll();

        var spinnerBar = new SpinnerView
        {
            X = Pos.Center(),
            Y = Pos.Center(),
            Width = Dim.Auto(),
            Height = Dim.Auto(),
            Visible = true,
            Style = new SpinnerStyle.Dots(),
            SpinDelay = 50,
            AutoSpin = true,
        };

        window.Add(spinnerBar);

        Application.Refresh();

        return spinnerBar;
    }
}
