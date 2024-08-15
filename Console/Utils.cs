using System.Reflection;
using DbUp;
using NativeLibraryManager;
using PortAudioSharp;
using Terminal.Gui;

namespace Console;

public static class Utils
{
    private static bool _isShowing = false;

    public static void ConfigurePlatformDependencies()
    {
        ResourceAccessor accessor = new(Assembly.GetExecutingAssembly());

        LibraryItem winLib = Environment.Is64BitProcess
            ? new LibraryItem(
                Platform.Windows,
                Bitness.x64,
                new LibraryFile("portaudio.dll", accessor.Binary("portaudio.dll"))
            )
            : new LibraryItem(
                Platform.Windows,
                Bitness.x32,
                new LibraryFile("portaudio.dll", accessor.Binary("portaudio.dll"))
            );

        LibraryManager libManager =
            new(
                new LibraryItem(
                    Platform.Linux,
                    Bitness.x64,
                    new LibraryFile("libportaudio.so", accessor.Binary("libportaudio.so"))
                ),
                new LibraryItem(
                    Platform.MacOs,
                    Bitness.x64,
                    new LibraryFile("libportaudio.dylib", accessor.Binary("libportaudio.dylib"))
                ),
                winLib
            );
        libManager.LoadNativeLibrary();
        PortAudio.Initialize();
    }

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

    public static string? ShowInputDialog(string title, string prompt, ColorScheme colorScheme)
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
            ColorScheme = colorScheme
        };
        var input = new TextField()
        {
            X = Pos.Center(),
            Y = 2,
            Width = 10,
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
