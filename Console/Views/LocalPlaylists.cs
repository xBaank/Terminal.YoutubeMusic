using System.Data;
using Console.Audio;
using Console.Database;
using Console.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Terminal.Gui;

namespace Console.Views;

internal class LocalPlaylistsView
{
    private readonly View _view;
    private readonly QueueView _queueView;
    private readonly TableView _tableView;
    private readonly IServiceProvider _serviceProvider;
    private IReadOnlyCollection<string> _playlistsNames;

    public LocalPlaylistsView(
        View view,
        QueueView queueView,
        PlayerController playerController,
        SharedCancellationTokenSource sharedCancellationTokenSource,
        IServiceProvider serviceProvider
    )
    {
        _serviceProvider = serviceProvider;
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

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
                    var songs = db.PlaylistSongs.Select(i => i.Song).ToList();

                    queueView.ChangeTitle(item);
                    await playerController.SetAsync(songs, sharedCancellationTokenSource.Token);
                    await playerController.PlayAsync();
                    Application.Invoke(() => queueView.HideLoading());
                }
                catch (TaskCanceledException) { }
            });
        };

        _playlistsNames = db.Playlists.Select(i => i.Name).ToList();
        _view = view;
        _queueView = queueView;
    }

    public void ShowLocalPlaylists()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        _view.RemoveAll();
        var dataTable = new DataTable();
        dataTable.Columns.Add("Title", typeof(string));
        _playlistsNames = db.Playlists.Select(i => i.Name).ToList();
        foreach (var item in _playlistsNames)
        {
            dataTable.Rows.Add(item);
        }
        _tableView.Table = new DataTableSource(dataTable);
        _view.Add(_tableView);
    }
}
