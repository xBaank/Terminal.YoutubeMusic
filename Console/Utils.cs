using System.Runtime.InteropServices;
using System.Text.Json;
using Console.LocalPlaylists;
using OpenTK.Audio.OpenAL;
using Terminal.Gui;

namespace Console;

public static class Utils
{
    public const string PlaylistDirectiory = "Playlists";
    public static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
    {
        Converters = { new VideoIdConverter() },
    };

    private static bool _isShowing = false;

    public static void ConfigurePlatformDependencies()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            OpenALLibraryNameContainer.OverridePath = "libopenal.so";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            OpenALLibraryNameContainer.OverridePath = "libopenal.dylib";
        }
        else
        {
            OpenALLibraryNameContainer.OverridePath = "soft_oal.dll";
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
