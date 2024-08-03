using System.Data;
using Console.Audio;
using Console.Extensions;
using Terminal.Gui;

namespace Console.Views;

internal class RecommendationsView(
    View view,
    PlayerController playerController,
    QueueView queueView,
    SharedCancellationTokenSource sharedCancellationTokenSource
) : Loader(view)
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
                FullRowSelect = true,
                Table = new DataTableSource(dataTable)
            }
                .WithPos(0)
                .WithFill()
                .WithCleanStyle();

            tableView.CellActivated += async (_, args) =>
            {
                var item = recommendations.ElementAtOrDefault(args.Row);
                if (item is null)
                    return;

                sharedCancellationTokenSource.Cancel();
                sharedCancellationTokenSource.Reset();
                await Task.Run(async () =>
                {
                    try
                    {
                        Application.Invoke(() => queueView.ShowLoading());
                        queueView.ChangeTitle(item.Title);
                        await playerController.SetAsync(item, sharedCancellationTokenSource.Token);
                        await playerController.PlayAsync();
                        Application.Invoke(() => queueView.HideLoading());
                    }
                    catch (TaskCanceledException) { }
                });
            };

            Application.Invoke(() => view.Add(tableView));
        });
}
