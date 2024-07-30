using System.Data;
using Console.Audio;
using Terminal.Gui;
using YoutubeExplode.Search;

namespace Console.Views;

internal class RecommendationsView(View view, PlayerController playerController) : Loader(view)
{
    public void ShowRecommendations() =>
        Task.Run(async () =>
        {
            Application.Invoke(() => ShowLoading());
            var recommendations = await playerController.GetRecommendationsAsync();
            Application.Invoke(() => HideLoading());
            Application.Invoke(() => view.RemoveAll());

            var dataTable = new DataTable();

            dataTable.Columns.Add("Title", typeof(string));
            dataTable.Columns.Add("Description", typeof(string));

            foreach (var item in recommendations)
            {
                dataTable.Rows.Add(item.Title, item.Subtitle);
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

            tableView.CellActivated += async (_, args) =>
            {
                var item = recommendations.ElementAtOrDefault(args.Row);
                if (item is null)
                    return;

                await Task.Run(async () =>
                {
                    await playerController.SetAsync(item);
                    await playerController.PlayAsync();
                });
            };

            Application.Invoke(() => view.Add(tableView));
        });
}
