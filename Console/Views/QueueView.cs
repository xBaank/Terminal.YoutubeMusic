using System.Collections.ObjectModel;
using Console.Audio;
using Console.Extensions;
using Terminal.Gui;

namespace Console.Views;

internal class QueueView(View view, PlayerController playerController) : Loader(view)
{
    private ListView _listView =
        new()
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

    public override void HideLoading()
    {
        UpdateList();
        base.HideLoading();
    }

    public async Task SavePlaylist()
    {
        var name = Utils.ShowInputDialog(
            "Playlist name",
            "Give the playlist a name",
            View.ColorScheme
        );
        if (name is null)
            return;

        throw new NotImplementedException();
    }

    void UpdateList()
    {
        _listView.SetSource(
            new ObservableCollection<string>(
                playerController
                    .Songs.Select(
                        (i, index) =>
                        {
                            return playerController.Song == i
                                ? $"Playing [{index}] {i.Title.Sanitize()}"
                                : $"[{index}] {i.Title.Sanitize()}";
                        }
                    )
                    .ToList()
            )
        );

        View.RemoveAll();
        View.Add(_listView);
    }

    public void ChangeTitle(string text) => View.Title = $"Playlist: {text}";

    public void ShowQueue()
    {
        _listView.OpenSelectedItem += async (_, args) =>
        {
            var song = playerController.Songs.ElementAtOrDefault(args.Item);

            if (song is null)
                return;

            await Task.Run(async () =>
            {
                await playerController.SkipToAsync(song);
                await playerController.PlayAsync();
            });
        };

        playerController.StateChanged += UpdateList;
        playerController.QueueChanged += (_) => UpdateList();
    }
}
