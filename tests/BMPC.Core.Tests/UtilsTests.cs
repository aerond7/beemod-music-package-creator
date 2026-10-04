using BMPC.Core;

namespace BMPC.Core.Tests;

public class UtilsTests
{
    [Fact]
    public void ConvertToSafeFileName_RemovesSpacesPunctuationAndLowercases()
    {
        var result = Utils.ConvertToSafeFileName("  A B,C;D'E.F  ");

        Assert.Equal("abcdef", result);
    }

    [Fact]
    public void ConvertToSafeFileName_RemovesInvalidFileNameCharacters()
    {
        var invalidCharacters = new string(Path.GetInvalidFileNameChars());

        var result = Utils.ConvertToSafeFileName($"Good{invalidCharacters}Name");

        Assert.Equal("goodname", result);
    }

    [Theory]
    [InlineData("???")]
    [InlineData(" . , ; ' ")]
    [InlineData("<>|*")]
    public void GetEmptySafeNameError_WhenOnlyStrippedCharacters_ReturnsError(string name)
    {
        var error = Utils.GetEmptySafeNameError("Music name", name);

        Assert.NotNull(error);
        Assert.StartsWith("Music name", error);
    }

    [Fact]
    public void GetEmptySafeNameError_WhenNameKeepsCharacters_ReturnsNull()
    {
        Assert.Null(Utils.GetEmptySafeNameError("Package name", "My Pack!"));
    }

    [Theory]
    [InlineData("mysong")]
    [InlineData("My.Song")]
    [InlineData("MY SONG?")]
    public void GetDuplicateSongNameError_WhenSafeNamesMatch_NamesConflictingSong(string name)
    {
        var error = Utils.GetDuplicateSongNameError(name, ["Other", "My Song"]);

        Assert.NotNull(error);
        Assert.Contains("\"My Song\"", error);
    }

    [Fact]
    public void GetDuplicateSongNameError_WhenSafeNamesDiffer_ReturnsNull()
    {
        Assert.Null(Utils.GetDuplicateSongNameError("My Song!", ["My Song 2", "Other"]));
    }

    [Fact]
    public void EscapeString_EscapesQuotesBackslashesAndNewlines()
    {
        var result = Utils.EscapeString("quote \" slash \\ line\nnext");

        Assert.Equal("quote \\\" slash \\\\ line\\nnext", result);
    }

    [Fact]
    public void TryDeleteDirectory_WhenMissing_ReturnsTrue()
    {
        var path = Path.Combine(Path.GetTempPath(), "bmpc-tests", Guid.NewGuid().ToString("N"));

        Assert.True(Utils.TryDeleteDirectory(path));
    }

    [Fact]
    public void TryDeleteDirectory_DeletesNestedContents()
    {
        var path = Path.Combine(Path.GetTempPath(), "bmpc-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(path, "sub"));
        File.WriteAllText(Path.Combine(path, "a.txt"), "a");
        File.WriteAllText(Path.Combine(path, "sub", "b.txt"), "b");

        Assert.True(Utils.TryDeleteDirectory(path));
        Assert.False(Directory.Exists(path));
    }

    [Fact]
    public void TryDeleteDirectory_WhenFileLocked_SkipsItAndDeletesTheRest()
    {
        var path = Path.Combine(Path.GetTempPath(), "bmpc-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(path, "sub"));
        var lockedFile = Path.Combine(path, "sub", "locked.txt");
        var otherFile = Path.Combine(path, "other.txt");
        File.WriteAllText(lockedFile, "locked");
        File.WriteAllText(otherFile, "other");

        try
        {
            using (new FileStream(lockedFile, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Assert.False(Utils.TryDeleteDirectory(path));
                Assert.True(File.Exists(lockedFile));
                Assert.False(File.Exists(otherFile));
            }

            Assert.True(Utils.TryDeleteDirectory(path));
            Assert.False(Directory.Exists(path));
        }
        finally
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Plain ASCII ~!@#$%^&*()_+-={}[]|\\:\";'<>?,./`")]
    [InlineData("tab\tnew\r\nline")]
    public void FindNonAsciiCharacter_WhenAsciiOnly_ReturnsNull(string? value)
    {
        Assert.Null(Utils.FindNonAsciiCharacter(value));
    }

    [Theory]
    [InlineData("Café", "é")]
    [InlineData("Björk and Bjørn", "ö")]
    [InlineData("music \U0001F3B5 note", "\U0001F3B5")]
    [InlineData("non breaking", " ")]
    public void FindNonAsciiCharacter_ReturnsFirstNonAsciiCharacter(string value, string expected)
    {
        Assert.Equal(expected, Utils.FindNonAsciiCharacter(value));
    }

    [Fact]
    public void GetNonAsciiTextError_WhenAsciiOnly_ReturnsNull()
    {
        Assert.Null(Utils.GetNonAsciiTextError("Package name", "Cafe"));
    }

    [Fact]
    public void GetNonAsciiTextError_NamesFieldAndCharacter()
    {
        var result = Utils.GetNonAsciiTextError("Package name", "Café");

        Assert.NotNull(result);
        Assert.StartsWith("Package name contains \"é\"", result);
    }

    [Fact]
    public void EscapeString_EscapesUnicodeCharacters()
    {
        var result = Utils.EscapeString("snowman \u2603 tab\t");

        Assert.Equal("snowman \\u2603 tab\\t", result);
    }
}
