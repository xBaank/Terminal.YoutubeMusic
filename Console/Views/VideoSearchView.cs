using System.Threading;
using Console.Audio;
using Terminal.Gui;

namespace Console.Views;

internal class VideoSearchView(Window win, VideosResultsView videosResults, PlayerController player)
{
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    public void ShowSearch()
    {
        win.RemoveAll();

        var textField = new TextField
        {
            X = 0,
            Y = Pos.Center(),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        textField.KeyUp += (_, args) =>
        {
            if (args.KeyCode != Key.Enter)
                return;

            Application.Invoke(async () =>
            {
                var text = textField.Text.ToString();

                if (text is null)
                    return;

                cancellationTokenSource.Cancel();
                cancellationTokenSource = new CancellationTokenSource();
                try
                {
                    videosResults.SetFocus();
                    videosResults.ShowLoading();
                    var results = await player.SearchAsync(text, cancellationTokenSource.Token);
                    videosResults.HideLoading();
                    videosResults.ShowVideos(results);
                }
                catch (TaskCanceledException) { }
            });
        };

        win.Add(textField);
    }
}
