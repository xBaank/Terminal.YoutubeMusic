using Console;
using Console.Audio;
using FluentAssertions;
using OpenTK.Audio.OpenAL;
using YoutubeExplode;

namespace Tests;

public class PlayerTests : IAsyncDisposable
{
    private readonly PlayerController _player;

    public PlayerTests()
    {
        Utils.ConfigurePlatformDependencies();
        _player = new(new YoutubeClient()) { Volume = 0 };
    }

    [Fact]
    public async Task I_can_play_a_song()
    {
        var finishTask = new TaskCompletionSource();
        _player.OnFinish += finishTask.SetResult;

        var video = (
            await _player.SearchAsync("https://www.youtube.com/watch?v=f8mL0_4GeV0")
        ).First();

        await _player.SetAsync(video);
        await _player.PlayAsync();

        await finishTask.Task;
        _player.State.Should().Be(ALSourceState.Stopped);
        _player.Song.Should().Be(video);
    }

    [Fact]
    public async Task I_can_skip_a_song()
    {
        var finishTask = new TaskCompletionSource();

        var video = (
            await _player.SearchAsync("https://www.youtube.com/watch?v=f8mL0_4GeV0")
        ).First();

        await _player.SetAsync(video);
        await _player.PlayAsync();
        await _player.SkipAsync();

        _player.State.Should().Be(ALSourceState.Initial);
        _player.Song.Should().Be(null);
    }

    [Fact]
    public async Task I_can_pause_a_song()
    {
        var video = (
            await _player.SearchAsync("https://www.youtube.com/watch?v=ZKzmyGKWFjU")
        ).First();

        await _player.SetAsync(video);
        await _player.PlayAsync();
        await Task.Delay(5000);
        await _player.PauseAsync();

        _player.State.Should().Be(ALSourceState.Paused);
        _player.Song.Should().Be(video);
    }

    [Fact]
    public async Task I_can_stop_a_song()
    {
        var video = (
            await _player.SearchAsync("https://www.youtube.com/watch?v=ZKzmyGKWFjU")
        ).First();

        await _player.SetAsync(video);
        await _player.PlayAsync();
        await Task.Delay(5000);
        await _player.StopAsync();

        _player.State.Should().Be(ALSourceState.Stopped);
        _player.Song.Should().Be(video);
    }

    [Fact]
    public async Task I_can_set_another_song_while_playing()
    {
        var finishTask = new TaskCompletionSource();
        _player.OnFinish += finishTask.SetResult;

        var video = (
            await _player.SearchAsync("https://www.youtube.com/watch?v=ZKzmyGKWFjU")
        ).First();
        var video2 = (
            await _player.SearchAsync("https://www.youtube.com/watch?v=f8mL0_4GeV0")
        ).First();

        await _player.SetAsync(video);
        await _player.PlayAsync();
        await Task.Delay(5000);
        await _player.SetAsync(video2);
        await _player.PlayAsync();
        await finishTask.Task;

        _player.State.Should().Be(ALSourceState.Stopped);
        _player.Song.Should().Be(video2);
    }

    public async ValueTask DisposeAsync() => await _player.DisposeAsync();
}
