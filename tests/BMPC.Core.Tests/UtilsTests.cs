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

    [Fact]
    public void EscapeString_EscapesQuotesBackslashesAndNewlines()
    {
        var result = Utils.EscapeString("quote \" slash \\ line\nnext");

        Assert.Equal("quote \\\" slash \\\\ line\\nnext", result);
    }

    [Fact]
    public void EscapeString_EscapesUnicodeCharacters()
    {
        var result = Utils.EscapeString("snowman \u2603 tab\t");

        Assert.Equal("snowman \\u2603 tab\\t", result);
    }
}
