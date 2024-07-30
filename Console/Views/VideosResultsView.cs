using System.Data;
using Console.Audio;
using Console.Extensions;
using Terminal.Gui;
using YoutubeExplode.Search;

namespace Console.Views;

internal class VideosResultsView(Tab tab, TabView tabView, PlayerController playerController)
    : Loader(tab.View)
{
    private View View => tab.View;

    public void SetFocus() => tabView.SelectedTab = tab;

    public void ShowVideos(List<ISearchResult> videoSearches)
    {
        View.RemoveAll();

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
                    $"{playlistSearchResult?.Count.ToString() ?? "???"} videos"
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

        View.Add(tableView);

        tableView.CellActivated += async (_, args) =>
        {
            var item = videoSearches.ElementAtOrDefault(args.Row);
            if (item is null)
                return;

            await Task.Run(async () =>
            {
                await playerController.SetAsync(item);
                await playerController.PlayAsync();
            });
        };
    }
}
