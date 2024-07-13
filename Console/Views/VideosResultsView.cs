using System.Data;
using Console.Audio;
using Console.Extensions;
using Terminal.Gui;
using YoutubeExplode.Search;

namespace Console.Views;

public class VideosResultsView(Window win, PlayerController playerController)
{
    private CancellationTokenSource tokenSource = new();
    private Task? loadingTask = null;

    public void ShowLoading()
    {
        tokenSource = new CancellationTokenSource();
        var progressBar = win.DisplayProgressBar();
        loadingTask = progressBar.Start(tokenSource.Token);
    }

    public async Task HideLoading()
    {
        tokenSource.Cancel();
        if (loadingTask is not null)
            await loadingTask.ConfigureAwait(false);
    }

    public void ShowVideos(List<VideoSearchResult> videoSearches)
    {
        win.RemoveAll();

        var dataTable = new DataTable();

        dataTable.Columns.Add("Title", typeof(string));
        dataTable.Columns.Add("Author", typeof(string));
        dataTable.Columns.Add("Duration", typeof(string));

        videoSearches.ForEach(x =>
            dataTable.Rows.Add(
                x.Title,
                x.Author,
                x.Duration.GetValueOrDefault().ToString(@"hh\:mm\:ss")
            )
        );

        var tableView = new TableView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            FullRowSelect = true,
            Table = new DataTableSource(dataTable)
        };

        win.Add(tableView);

        tableView.CellActivated += async (_, args) =>
        {
            var item = videoSearches[args.Row];

            await Task.Run(async () =>
            {
                await playerController.AddAsync(item!.Id);
                await playerController.PlayAsync();
            });
        };
    }
}
