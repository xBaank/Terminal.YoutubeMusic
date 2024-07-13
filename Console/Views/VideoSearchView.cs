using Console.Audio;
using Terminal.Gui;

namespace Console.Views;

public class VideoSearchView(Window win, VideosResultsView videosResults, PlayerController player)
{
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

                videosResults.ShowLoading();
                var results = await player.Search(text);
                await videosResults.HideLoading();

                videosResults.ShowVideos(results);
            });
        };

        win.Add(textField);
    }
}
