using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace Console.Extensions;

internal static class ViewExtensions
{
    public static T WithPos<T>(this T view, Pos pos)
        where T : View
    {
        view.X = pos;
        view.Y = pos;
        return view;
    }

    public static T WithFill<T>(this T view)
        where T : View
    {
        view.Height = Dim.Fill();
        view.Width = Dim.Fill();
        return view;
    }

    public static TableView WithCleanStyle(this TableView view)
    {
        view.Style.ShowHorizontalHeaderUnderline = true;
        view.Style.ShowHorizontalBottomline = false;
        view.Style.ShowHorizontalHeaderOverline = false;
        view.Style.ShowVerticalHeaderLines = false;
        view.Style.ShowVerticalCellLines = false;
        return view;
    }
}
