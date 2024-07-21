using Console;
using Console.Audio;
using FluentAssertions;
using OpenTK.Audio.OpenAL;

namespace Tests;

public class PlayerTests
{
    public PlayerTests() => Utils.ConfigurePlatformDependencies();

    [Fact]
    public async Task I_can_play_a_song()
    {
        await using var player = new PlayerController() { Volume = 0 };

        var video = (
            await player.SearchAsync("https://www.youtube.com/watch?v=f8mL0_4GeV0")
        ).First();

        await player.SetAsync(video);
        await player.PlayAsync();
        await Task.Delay((player.TotalTime ?? default) + TimeSpan.FromSeconds(5));

        player.State.Should().Be(ALSourceState.Stopped);
        player.Song.Should().Be(video);
    }
}
