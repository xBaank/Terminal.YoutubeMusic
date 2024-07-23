using Console;
using Console.Audio;
using FluentAssertions;
using OpenTK.Audio.OpenAL;

namespace Tests;

public class PlayerTests
{
    public PlayerTests()
    {
        Utils.ConfigurePlatformDependencies();
        Utils.DeviceName = Environment.GetEnvironmentVariable("DeviceName");
    }

    [Fact]
    public async Task I_can_play_a_song()
    {
        await using var player = new PlayerController() { Volume = 0 };
        var finishTask = new TaskCompletionSource();
        player.OnFinish += finishTask.SetResult;

        var video = (
            await player.SearchAsync("https://www.youtube.com/watch?v=f8mL0_4GeV0")
        ).First();

        await player.SetAsync(video);
        await player.PlayAsync();

        await finishTask.Task;
        player.State.Should().Be(ALSourceState.Stopped);
        player.Song.Should().Be(video);
    }

    [Fact]
    public async Task I_can_skip_a_song()
    {
        await using var player = new PlayerController() { Volume = 0 };
        var finishTask = new TaskCompletionSource();

        var video = (
            await player.SearchAsync("https://www.youtube.com/watch?v=f8mL0_4GeV0")
        ).First();

        await player.SetAsync(video);
        await player.PlayAsync();
        await player.SkipAsync();

        player.State.Should().Be(ALSourceState.Initial);
        player.Song.Should().Be(null);
    }

    [Fact]
    public async Task I_can_pause_a_song()
    {
        await using var player = new PlayerController() { Volume = 0 };

        var video = (
            await player.SearchAsync("https://www.youtube.com/watch?v=ZKzmyGKWFjU")
        ).First();

        await player.SetAsync(video);
        await player.PlayAsync();
        await Task.Delay(5000);
        await player.PauseAsync();

        player.State.Should().Be(ALSourceState.Paused);
        player.Song.Should().Be(video);
    }

    [Fact]
    public async Task I_can_stop_a_song()
    {
        await using var player = new PlayerController() { Volume = 0 };

        var video = (
            await player.SearchAsync("https://www.youtube.com/watch?v=ZKzmyGKWFjU")
        ).First();

        await player.SetAsync(video);
        await player.PlayAsync();
        await Task.Delay(5000);
        await player.StopAsync();

        player.State.Should().Be(ALSourceState.Stopped);
        player.Song.Should().Be(video);
    }

    [Fact]
    public async Task I_can_set_another_song_while_playing()
    {
        await using var player = new PlayerController() { Volume = 0 };
        var finishTask = new TaskCompletionSource();
        player.OnFinish += finishTask.SetResult;

        var video = (
            await player.SearchAsync("https://www.youtube.com/watch?v=ZKzmyGKWFjU")
        ).First();
        var video2 = (
            await player.SearchAsync("https://www.youtube.com/watch?v=f8mL0_4GeV0")
        ).First();

        await player.SetAsync(video);
        await player.PlayAsync();
        await Task.Delay(5000);
        await player.SetAsync(video2);
        await player.PlayAsync();
        await finishTask.Task;

        player.State.Should().Be(ALSourceState.Stopped);
        player.Song.Should().Be(video2);
    }
}
