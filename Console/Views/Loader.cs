using Terminal.Gui;

namespace Console.Views;

internal abstract class Loader(View view)
{
    private object _lock = new();
    private SpinnerView? spinner = null;

    public View View => view;

    public virtual void ShowLoading()
    {
        View.RemoveAll();

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

        View.Add(spinner);
    }

    public virtual void HideLoading()
    {
        spinner?.Dispose();
        View.Remove(spinner);
    }
}
