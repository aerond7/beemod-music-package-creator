using BMPC.Core.Beemod;

namespace BMPC.Core.Tests;

public class BeemodMetaBuilderTests
{
    [Fact]
    public async Task Build_WritesExpectedMetadata()
    {
        var builder = new BeemodMetaBuilder()
            .WithName("Test Package, One")
            .WithDescription("Desc with \"quote\" and snow \u2603")
            .AddMusic(new BeemodPackMusic
            {
                Name = "Base Song",
                ShortName = "Base",
                Group = "Puzzle Group",
                Icon = "icons/base.png",
                IconLarge = "icons/base_large.png",
                Authors = "Author \\ Name",
                Description = "Line one\nLine two",
                SampleBase = "samples/base.wav",
                SampleTractorBeam = "samples/tbeam.wav",
                SoundscriptBase = "scripts/base.txt",
                SoundscriptTractorBeam = "scripts/tbeam.txt",
                SyncTractorBeamMusic = true,
                SpeedGelSfx = new List<string> { "sfx/speed_1.wav", "sfx/speed_2.wav" },
                BounceGelSfx = new List<string> { "sfx/bounce.wav" }
            })
            .AddMusic(new BeemodPackMusic
            {
                Name = "Default Funnel",
                Group = "Puzzle Group",
                Icon = "icons/default.png",
                IconLarge = "icons/default_large.png",
                Authors = "BMPC",
                Description = "Uses default tractor beam child",
                SampleBase = "samples/default.wav",
                SoundscriptBase = "scripts/default.txt",
                UseDefaultTractorBeamMusic = true
            });

        using var stream = builder.Build();
        using var reader = new StreamReader(stream);
        var actual = await reader.ReadToEndAsync();

        var expected = await File.ReadAllTextAsync("Fixtures/beemod-info-basic.expected.txt");
        Assert.Equal(Normalize(expected), Normalize(actual));
    }

    private static string Normalize(string value)
        => value.Replace("\r\n", "\n", StringComparison.Ordinal);
}
