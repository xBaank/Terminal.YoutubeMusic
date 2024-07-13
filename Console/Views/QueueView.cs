using System.Collections.ObjectModel;
using Console.Audio;
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
            Height = Dim.Fill()
        };

        win.Add(listView);

        playerController.QueueChanged += (queue) =>
        {
            listView.SetSource(
                new ObservableCollection<string>(queue.Select(i => i.Title).ToList())
            );
        };
    }
}
