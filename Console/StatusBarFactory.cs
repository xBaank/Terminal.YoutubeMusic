using System.Globalization;
using Console.Audio;
using Console.Views;
using Terminal.Gui;

namespace Console;

internal class StatusBarFactory(
    View searchView,
    View playerView,
    QueueView queueView,
    TabView tabView,
    PlayerController playerController
)
{
    public StatusBar Create() =>
        new(
            [
                new Shortcut(Key.Esc, "Exit", () => { }),
                new Shortcut(Key.Q.WithCtrl, "Search", searchView.SetFocus),
                new Shortcut(
                    Key.L.WithCtrl,
                    "Rotate tabs",
                    () =>
                    {
                        var current = tabView.Tabs.ToList().IndexOf(tabView.SelectedTab);
                        if (current == tabView.Tabs.Count - 1)
                        {
                            tabView.SelectedTab = tabView.Tabs.ElementAt(0);
                        }
                        else
                        {
                            tabView.SelectedTab = tabView.Tabs.ElementAt(current + 1);
                        }
                        tabView.SetFocus();
                        tabView.EnsureSelectedTabIsVisible();
                    }
                ),
                new Shortcut(Key.P.WithCtrl, "Player", playerView.SetFocus),
                new Shortcut(Key.M.WithCtrl, "Playlist", queueView.View.SetFocus),
                new Shortcut(
                    Key.P.WithAlt,
                    "Save Playlist",
                    async () => await queueView.SavePlaylist()
                ),
                new Shortcut(
                    Key.Space.WithCtrl,
                    "Seek",
                    async () =>
                    {
                        var result = Utils.ShowInputDialog(
                            "Seek time",
                            "Enter seek time with the format : HH:MM:SS",
                            queueView.View.ColorScheme
                        );

                        if (result is null)
                        {
                            return;
                        }

                        var isParsed = TimeSpan.TryParseExact(
                            result,
                            "g",
                            CultureInfo.InvariantCulture,
                            out var time
                        );

                        if (isParsed)
                        {
                            await playerController.SeekAsync(time);
                        }
                    }
                ),
            ]
        );
}
