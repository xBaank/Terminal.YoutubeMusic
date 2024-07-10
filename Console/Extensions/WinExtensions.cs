using Terminal.Gui;

namespace Console.Extensions;

public static class WinExtensions
{
    public static ProgressBar DisplayProgressBar(this Window window)
    {
        window.RemoveAll();

        var progressBar = new ProgressBar()
        {
            X = Pos.Center(),
            Y = Pos.Center(),
            Width = 40,
            Visible = true,
            Fraction = 0.0f,
            ProgressBarStyle = ProgressBarStyle.Continuous,
            ColorScheme = new ColorScheme()
            {
                Normal = Application.Driver.MakeAttribute(Color.Red, Color.Black)
            }
        };

        window.Add(progressBar);
        Application.Refresh();

        return progressBar;
    }
}
