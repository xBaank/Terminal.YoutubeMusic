using System.Data;
using Console.Audio;
using Console.Extensions;
using Terminal.Gui;
using YoutubeExplode.Search;

namespace Console.Views;

public class VideosResultsView(Window win, PlayerController playerController)
{
    private SpinnerView? spinner = null;

    public void ShowLoading()
    {
        win.RemoveAll();

        spinner?.Dispose();
        spinner = new SpinnerView
        {
            X = Pos.Center(),
            Y = Pos.Center(),
            Width = Dim.Auto(),
            Height = Dim.Auto(),
            Visible = true,
            Style = new SpinnerStyle.BouncingBall(),
            AutoSpin = true,
        };

        win.Add(spinner);
    }

    public void HideLoading()
    {
        spinner?.Dispose();
        win.Remove(spinner);
    }

    public void ShowVideos(List<ISearchResult> videoSearches)
    {
        win.RemoveAll();

        var dataTable = new DataTable();

        dataTable.Columns.Add("Title", typeof(string));
        dataTable.Columns.Add("Author", typeof(string));
        dataTable.Columns.Add("Duration", typeof(string));

        foreach (var search in videoSearches)
        {
            if (search is VideoSearchResult videoSearchResult)
            {
                dataTable.Rows.Add(
                    videoSearchResult.Sanitize(),
                    videoSearchResult.Author.ChannelTitle.Sanitize(),
                    videoSearchResult.Duration.GetValueOrDefault().ToString(@"hh\:mm\:ss")
                );

                continue;
            }

            if (search is PlaylistSearchResult playlistSearchResult)
            {
                dataTable.Rows.Add(
                    playlistSearchResult.Sanitize(),
                    playlistSearchResult?.Author?.ChannelTitle?.Sanitize() ?? "",
                    ""
                );

                continue;
            }

            if (search is ChannelSearchResult channelSearchResult)
            {
                //Skip channels
                continue;
            }
        }

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
            var item = videoSearches.ElementAtOrDefault(args.Row);
            if (item is null)
                return;

            await Task.Run(async () =>
            {
                await playerController.AddAsync(item);
                await playerController.PlayAsync();
            });
        };
    }
}
