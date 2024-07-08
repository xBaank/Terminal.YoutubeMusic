using Console.Extensions;
using NAudio.Wave;
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
            Width = 40
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

            var results = await _youtubeClient.Search.GetVideosAsync(text).Take(20).ToListAsync();
            tokenSource.Cancel();
            _win.RemoveAll();

            var listView = new ListView(results)
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            _win.Add(listView);

            listView.OpenSelectedItem += async (args) =>
            {
                var tokenSource = new CancellationTokenSource();
                var progressBar = _win.DisplayProgressBar();
                var task = progressBar.Start(tokenSource.Token);
                var item = args.Value as VideoSearchResult;

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
