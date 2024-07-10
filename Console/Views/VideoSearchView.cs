using Terminal.Gui;

namespace Console.Views;

public class VideoSearchView(Window win, VideosResultsView videosResults, PlayerController player)
{
    public void ShowSearch()
    {
        win.RemoveAll();

        var textField = new TextField("")
        {
            X = 0,
            Y = Pos.Center(),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        textField.KeyPress += (args) =>
        {
            if (args.KeyEvent.Key != Key.Enter)
                return;

            Application.MainLoop.Invoke(async () =>
            {
                var text = textField.Text.ToString();

                if (text is null)
                    return;

                videosResults.ShowLoading();
                var results = await player.Search(text);
                await videosResults.HideLoading();

                videosResults.ShowVideos(results);
            });
        };

        win.Add(textField);
    }
}
