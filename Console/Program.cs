using Console;
using Terminal.Gui;

Application.Init();
var top = Application.Top;

var customColors = new ColorScheme()
{
    Normal = Application.Driver.MakeAttribute(Color.Red, Color.Black),
    Focus = Application.Driver.MakeAttribute(Color.White, Color.Red),
};

// Create a window and set its properties
var win = new Window("Youtube console")
{
    X = 0,
    Y = 1, // Leave one row for the toplevel menu
    Width = Dim.Fill(),
    Height = Dim.Fill(),
    ColorScheme = customColors
};
top.Add(win);

var options = new Options(win);
options.ShowOptions();

Application.Run();
