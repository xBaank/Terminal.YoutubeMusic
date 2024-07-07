using NAudio.Wave;
using Spectre.Console;
using YoutubeExplode;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;

var youtubeClient = new YoutubeClient();

var input = AnsiConsole.Ask<string>("[red]Search videos:[/]");

var videos = await AnsiConsole
    .Status()
    .AutoRefresh(true)
    .Spinner(Spinner.Known.BouncingBall)
    .SpinnerStyle(Style.Parse("red"))
    .StartAsync(
        $"Searching for {input.EscapeMarkup()} ...",
        async ctx =>
        {
            var videos = await youtubeClient
                .Search.GetVideosAsync(input)
                .Take(100)
                .Select(i => new VideoSearchResult(
                    i.Id,
                    i.Title.EscapeMarkup(),
                    i.Author,
                    i.Duration,
                    i.Thumbnails
                ))
                .ToListAsync();
            return videos;
        }
    );

AnsiConsole.Clear();

var selectedVideo = AnsiConsole.Prompt(
    new SelectionPrompt<VideoSearchResult>()
        .Title($"Videos found for {input.EscapeMarkup()}")
        .PageSize(10)
        .MoreChoicesText("[grey](Move up and down to reveal more video)[/]")
        .AddChoices(videos)
        .HighlightStyle(new Style(foreground: Color.Red, background: Color.Yellow))
);

var video = await youtubeClient.Videos.GetAsync(selectedVideo.Id);
var stream = await youtubeClient.Videos.Streams.GetManifestAsync(video.Id);
var bestAudio = stream.GetAudioOnlyStreams().MaxBy(i => i.Bitrate);

if (bestAudio is null)
{
    AnsiConsole.Markup("[yellow]Couldn't retreive the audio[/], [red]Quitting[/]");
    return;
}

using (var audioStream = new MediaFoundationReader(bestAudio.Url))
using (var outputDevice = new WaveOutEvent())
{
    // Initialize the output device
    outputDevice.Init(audioStream);

    await AnsiConsole
        .Status()
        .AutoRefresh(true)
        .Spinner(Spinner.Known.Moon)
        .SpinnerStyle(Style.Parse("red"))
        .StartAsync(
            $"[green]Playing: {video.Title.EscapeMarkup()}[/]",
            async ctx =>
            {
                outputDevice.Play();
                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    await Task.Delay(1000);
                }
            }
        );
}
