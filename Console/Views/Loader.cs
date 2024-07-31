using System.Runtime.CompilerServices;
using Terminal.Gui;

namespace Console.Views;

internal abstract class Loader(View view)
{
    private object _lock = new();
    private SpinnerView? spinner = null;

    public virtual void ShowLoading()
    {
        view.RemoveAll();

        spinner?.Dispose();
        spinner = new SpinnerView
        {
            X = Pos.Center(),
            Y = Pos.Center(),
            Width = Dim.Auto(),
            Height = Dim.Auto(),
            Visible = true,
            Style = new SpinnerStyle.BouncingBall(),
            AutoSpin = true,
        };

        view.Add(spinner);
    }

    public virtual void HideLoading()
    {
        spinner?.Dispose();
        view.Remove(spinner);
    }
}
