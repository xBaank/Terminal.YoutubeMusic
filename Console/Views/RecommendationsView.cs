﻿using System.Data;
using Console.Audio;
using Terminal.Gui;
using YoutubeExplode.Search;

namespace Console.Views;

internal class RecommendationsView(View win, PlayerController playerController)
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

    public void ShowRecommendations() =>
        Task.Run(async () =>
        {
            Application.Invoke(() => ShowLoading());
            var recommendations = await playerController.GetRecommendationsAsync();
            Application.Invoke(() => HideLoading());
            Application.Invoke(() => win.RemoveAll());

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

            Application.Invoke(() => win.Add(tableView));
        });
}
