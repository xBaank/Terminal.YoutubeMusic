using System.Data;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Console.Extensions;
using NAudio.Wave;
using NStack;
using Terminal.Gui;
using YoutubeExplode;
using YoutubeExplode.Search;

namespace Console;

public class VideoSearch
{
    private readonly Window _win;
    private readonly YoutubeClient _youtubeClient = new();
    private readonly Player _player;

    public VideoSearch(Window win)
    {
        _player = new Player(_youtubeClient);
        _win = win;
    }

    public void ShowSearch()
    {
        _win.RemoveAll();

        var label = new Label("Video search") { X = 1, Y = 1 };
        _win.Add(label);

        // Create a text field
        var textField = new TextField("")
        {
            X = 1,
            Y = Pos.Bottom(label) + 1,
            Width = 40,
        };
        _win.Add(textField);

        // Create a button to submit the input
        var submitButton = new Button("search") { X = 1, Y = Pos.Bottom(textField) + 1 };

        // Action to perform when the button is clicked
        submitButton.Clicked += async () =>
        {
            var text = textField.Text.ToString();

            if (text is null)
                return;

            var tokenSource = new CancellationTokenSource();
            var progressBar = _win.DisplayProgressBar();
            var task = progressBar.Start(tokenSource.Token);

            var results = await _youtubeClient.Search.GetVideosAsync(text).Take(50).ToListAsync();
            tokenSource.Cancel();
            _win.RemoveAll();

            var dataTable = new DataTable();

            dataTable.Columns.Add("Title", typeof(string));
            dataTable.Columns.Add("Author", typeof(string));
            dataTable.Columns.Add("Duration", typeof(string));

            results.ForEach(x =>
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
                AutoSize = true,
                Table = dataTable
            };

            _win.Add(tableView);

            tableView.CellActivated += async (args) =>
            {
                var tokenSource = new CancellationTokenSource();
                var progressBar = _win.DisplayProgressBar();
                var task = progressBar.Start(tokenSource.Token);
                var item = results[args.Row];

                await _player.AddAsync(item!.Id);
                await _player.PlayAsync();

                tokenSource.Cancel();
                _win.RemoveAll();

                var songLabel = new Label($"Playing {_player.Song?.Title}")
                {
                    X = Pos.Center(),
                    Y = Pos.Center()
                };
                _win.Add(songLabel);
            };
        };
        _win.Add(submitButton);
    }
}
