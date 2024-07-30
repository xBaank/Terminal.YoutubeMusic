using System.Collections.ObjectModel;
using Console.Audio;
using Console.Extensions;
using Terminal.Gui;

namespace Console.Views;

public class QueueView(Window win, PlayerController playerController)
{
    public void ShowQueue()
    {
        win.RemoveAll();

        var listView = new ListView()
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        listView.KeyBindings.Clear();

        listView.SetSource(new ObservableCollection<string>());

        win.Add(listView);

        listView.OpenSelectedItem += async (_, args) =>
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

        void UpdateList()
        {
            listView.SetSource(
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
        }

        playerController.StateChanged += UpdateList;
        playerController.QueueChanged += (_) => UpdateList();
    }
}
