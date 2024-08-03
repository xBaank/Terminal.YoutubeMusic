using System.Data;
using Console.Audio;
using Console.Database;
using Console.Extensions;
using Console.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Nito.Disposables.Internals;
using Terminal.Gui;

namespace Console.Views;

internal class LocalPlaylistsView
{
    private readonly View _view;
    private readonly QueueView _queueView;
    private readonly TableView _tableView;
    private readonly IServiceProvider _serviceProvider;
    private IReadOnlyCollection<LocalPlaylist> _playlists;

    public LocalPlaylistsView(
        View view,
        QueueView queueView,
        PlayerController playerController,
        SharedCancellationTokenSource sharedCancellationTokenSource,
        IServiceProvider serviceProvider
    )
    {
        _serviceProvider = serviceProvider;

        _tableView = new TableView() { FullRowSelect = true }
            .WithPos(0)
            .WithFill()
            .WithCleanStyle();

        _tableView.CellActivated += async (_, args) =>
        {
            var item = _playlists?.ElementAtOrDefault(args.Row);
            if (item is null)
                return;

            using var scope = _serviceProvider.CreateScope();
            using var localPlaylistsRepository =
                scope.ServiceProvider.GetRequiredService<LocalPlaylistsRepository>();

            sharedCancellationTokenSource.Cancel();
            sharedCancellationTokenSource.Reset();
            await Task.Run(async () =>
            {
                try
                {
                    Application.Invoke(() => queueView.ShowLoading());

                    var songs =
                        (await localPlaylistsRepository.GetPlaylist(item.PlaylistId))
                            ?.PlaylistSongs?.Select(i => i.Song)
                            .WhereNotNull()
                            .ToList() ?? [];

                    queueView.ChangeTitle(item.Name);
                    await playerController.SetAsync(songs, sharedCancellationTokenSource.Token);
                    await playerController.PlayAsync();
                    Application.Invoke(() => queueView.HideLoading());
                }
                catch (TaskCanceledException) { }
            });
        };

        using var scope = _serviceProvider.CreateScope();
        using var localPlaylistsRepository =
            scope.ServiceProvider.GetRequiredService<LocalPlaylistsRepository>();
        _playlists = localPlaylistsRepository.GetPlaylistsAsync().Result.ToList();
        _view = view;
        _queueView = queueView;
    }

    public async ValueTask ShowLocalPlaylists()
    {
        using var scope = _serviceProvider.CreateScope();
        var localPlaylistsRepository =
            scope.ServiceProvider.GetRequiredService<LocalPlaylistsRepository>();

        _view.RemoveAll();
        var dataTable = new DataTable();
        dataTable.Columns.Add("Title", typeof(string));
        dataTable.Columns.Add("Videos", typeof(int));
        _playlists = await localPlaylistsRepository.GetPlaylistsAsync();
        foreach (var item in _playlists)
        {
            dataTable.Rows.Add(item, item.PlaylistSongs.Count);
        }
        _tableView.Table = new DataTableSource(dataTable);
        _view.Add(_tableView);
    }
}
