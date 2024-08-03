using System.Data;
using Console.Audio;
using Console.Extensions;
using Console.LocalPlaylists;
using Terminal.Gui;

namespace Console.Views;

internal class LocalPlaylistsView : IDisposable
{
    private readonly FileSystemWatcher _watcher = new(Utils.PlaylistDirectiory);
    private readonly View _view;
    private readonly QueueView _queueView;
    private readonly TableView _tableView;
    private IReadOnlyCollection<string> _playlistsNames;

    public LocalPlaylistsView(
        View view,
        QueueView queueView,
        PlayerController playerController,
        SharedCancellationTokenSource sharedCancellationTokenSource
    )
    {
        _tableView = new TableView() { FullRowSelect = true }
            .WithPos(0)
            .WithFill()
            .WithCleanStyle();

        _tableView.CellActivated += async (_, args) =>
        {
            var item = _playlistsNames?.ElementAtOrDefault(args.Row);
            if (item is null)
                return;

            sharedCancellationTokenSource.Cancel();
            sharedCancellationTokenSource.Reset();
            await Task.Run(async () =>
            {
                try
                {
                    Application.Invoke(() => queueView.ShowLoading());
                    var songs = await PlaylistImporter.ImportAsync(item);

                    queueView.ChangeTitle(item);
                    await playerController.SetAsync(songs, sharedCancellationTokenSource.Token);
                    await playerController.PlayAsync();
                    Application.Invoke(() => queueView.HideLoading());
                }
                catch (TaskCanceledException) { }
            });
        };

        _playlistsNames = PlaylistImporter.ListPlaylists();
        _watcher.Changed += (_, _) =>
        {
            ShowLocalPlaylists();
        };

        // Set the filter and enable events
        _watcher.Filter = "*.json"; // Watch all files
        _watcher.IncludeSubdirectories = false; // Watch all subdirectories
        _watcher.EnableRaisingEvents = true;
        _view = view;
        _queueView = queueView;
    }

    public void ShowLocalPlaylists()
    {
        _view.RemoveAll();
        var dataTable = new DataTable();
        dataTable.Columns.Add("Title", typeof(string));
        _playlistsNames = PlaylistImporter.ListPlaylists();
        foreach (var item in _playlistsNames)
        {
            dataTable.Rows.Add(item);
        }
        _tableView.Table = new DataTableSource(dataTable);
        _view.Add(_tableView);
    }

    public void Dispose()
    {
        _watcher.Dispose();
    }
}
