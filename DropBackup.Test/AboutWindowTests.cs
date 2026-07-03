using DropBackup;
using Xunit;

namespace DropBackup.Test;

public class AboutWindowTests
{
    [Theory]
    [InlineData("1.0.0", "1.0.0")]
    [InlineData("1.2.3+abc1234", "1.2.3")]
    [InlineData("2.0.0-beta+build.99", "2.0.0-beta")]
    [InlineData("", "")]
    public void FormatVersion_StripsPlusMetadata(string input, string expected)
    {
        Assert.Equal(expected, AboutWindow.FormatVersion(input));
    }
}
