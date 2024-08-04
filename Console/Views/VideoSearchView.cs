using Console.Audio;
using Terminal.Gui;

namespace Console.Views;

internal class VideoSearchView(View view, VideosResultsView videosResults, PlayerController player)
{
    private CancellationTokenSource _cancellationTokenSource = new();

    public void ShowSearch()
    {
        view.RemoveAll();

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

                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
                try
                {
                    videosResults.SetFocus();
                    videosResults.ShowLoading();
                    var results = await player.SearchAsync(text, _cancellationTokenSource.Token);
                    videosResults.HideLoading();
                    videosResults.ShowVideos(results);
                }
                catch (TaskCanceledException) { }
            });
        };

        view.Add(textField);
    }
}
