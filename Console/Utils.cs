using System.Reflection;
using DbUp;
using PortAudioSharp;
using Terminal.Gui;

namespace Console;

public static class Utils
{
    private static bool _isShowing = false;

    public static void ConfigurePlatformDependencies()
    {
        PortAudio.Initialize();
    }

    public static readonly ColorScheme CustomColor =
        new()
        {
            Normal = new Terminal.Gui.Attribute(Color.Parse("#FFFFFF"), Color.Parse("#1C1C1C")), // White on Dark Gray
            HotNormal = new Terminal.Gui.Attribute(Color.Parse("#FFD700"), Color.Parse("#1C1C1C")), // Gold on Dark Gray
            Focus = new Terminal.Gui.Attribute(Color.Parse("#FF4500"), Color.Parse("#1C1C1C")), // OrangeRed on Dark Gray
            HotFocus = new Terminal.Gui.Attribute(Color.Parse("#FF6347"), Color.Parse("#1C1C1C")) // Tomato on Dark Gray
        };

    public static void PerformMigrations(string connectionString)
    {
        var upgrader = DeployChanges
            .To.SQLiteDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            throw result.Error;
        }
    }

    public static string? ShowInputDialog(string title, string prompt)
    {
        if (_isShowing)
            return null;

        _isShowing = true;

        var dialog = new Dialog
        {
            BorderStyle = LineStyle.Rounded,
            Height = Dim.Auto(),
            Width = 70,
            Title = title,
            ColorScheme = CustomColor
        };
        var input = new TextField()
        {
            X = Pos.Center(),
            Y = 2,
            Width = Dim.Fill(5),
            Height = 1,
        };
        var buttons = new View
        {
            X = Pos.Center(),
            Y = 3,
            Width = Dim.Auto(),
            Height = Dim.Auto(),
        };
        var okButton = new Button
        {
            Title = "Ok",
            X = 0,
            Y = 3
        };
        var cancelButton = new Button
        {
            Title = "Cancel",
            X = Pos.Right(okButton) + 2,
            Y = 3
        };
        buttons.Add(okButton, cancelButton);

        var label = new Label
        {
            Text = prompt,
            X = Pos.Center(),
            Y = 0
        };

        dialog.Add(label, input, buttons);

        string? result = null;

        okButton.Accept += (_, args) =>
        {
            result = input.Text.ToString();
            Application.RequestStop();
        };
        cancelButton.Accept += (_, args) => Application.RequestStop();

        Application.Run(dialog);

        _isShowing = false;

        return result;
    }
}
