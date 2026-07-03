using System.IO;
using System.Text;
using DropBackup;
using Xunit;

namespace DropBackup.Test;

public class IniFileTests : IDisposable
{
    private readonly string _iniPath;

    public IniFileTests()
    {
        _iniPath = Path.Combine(Path.GetTempPath(), $"IniFileTest_{Guid.NewGuid():N}.ini");
    }

    public void Dispose()
    {
        if (File.Exists(_iniPath))
            File.Delete(_iniPath);
    }

    // -------------------------------------------------------------------
    // GetPrivateProfileString
    // -------------------------------------------------------------------

    [Fact]
    public void GetPrivateProfileString_ReturnsWrittenValue()
    {
        IniFile.WritePrivateProfileString("Section", "Key", "Hello", _iniPath);
        var sb = new StringBuilder(100);
        IniFile.GetPrivateProfileString("Section", "Key", "default", sb, 100, _iniPath);
        Assert.Equal("Hello", sb.ToString());
    }

    [Fact]
    public void GetPrivateProfileString_ReturnsDefaultWhenKeyMissing()
    {
        IniFile.WritePrivateProfileString("Section", "Other", "x", _iniPath);
        var sb = new StringBuilder(100);
        IniFile.GetPrivateProfileString("Section", "Missing", "fallback", sb, 100, _iniPath);
        Assert.Equal("fallback", sb.ToString());
    }

    [Fact]
    public void GetPrivateProfileString_ReturnsDefaultWhenSectionMissing()
    {
        var sb = new StringBuilder(100);
        IniFile.GetPrivateProfileString("NoSection", "Key", "def", sb, 100, _iniPath);
        Assert.Equal("def", sb.ToString());
    }

    [Fact]
    public void GetPrivateProfileString_ReturnsDefaultWhenFileNotExist()
    {
        string nonExistent = _iniPath + ".missing";
        var sb = new StringBuilder(100);
        IniFile.GetPrivateProfileString("S", "K", "dv", sb, 100, nonExistent);
        Assert.Equal("dv", sb.ToString());
    }

    [Fact]
    public void GetPrivateProfileString_TruncatesToBufferSize()
    {
        IniFile.WritePrivateProfileString("S", "K", "ABCDEFGHIJ", _iniPath);
        var sb = new StringBuilder(10);
        IniFile.GetPrivateProfileString("S", "K", "", sb, 5, _iniPath);
        Assert.Equal("ABCD", sb.ToString());
    }

    // -------------------------------------------------------------------
    // GetPrivateProfileInt
    // -------------------------------------------------------------------

    [Fact]
    public void GetPrivateProfileInt_ReturnsWrittenInteger()
    {
        IniFile.WritePrivateProfileString("S", "Num", "42", _iniPath);
        uint result = IniFile.GetPrivateProfileInt("S", "Num", 0, _iniPath);
        Assert.Equal(42u, result);
    }

    [Fact]
    public void GetPrivateProfileInt_ReturnsDefaultWhenKeyMissing()
    {
        uint result = IniFile.GetPrivateProfileInt("S", "Missing", 99, _iniPath);
        Assert.Equal(99u, result);
    }

    [Fact]
    public void GetPrivateProfileInt_ReturnsDefaultWhenValueNotInteger()
    {
        IniFile.WritePrivateProfileString("S", "K", "notanumber", _iniPath);
        uint result = IniFile.GetPrivateProfileInt("S", "K", 7, _iniPath);
        Assert.Equal(7u, result);
    }

    // -------------------------------------------------------------------
    // WritePrivateProfileString
    // -------------------------------------------------------------------

    [Fact]
    public void WritePrivateProfileString_OverwritesExistingKey()
    {
        IniFile.WritePrivateProfileString("S", "K", "first", _iniPath);
        IniFile.WritePrivateProfileString("S", "K", "second", _iniPath);
        var sb = new StringBuilder(100);
        IniFile.GetPrivateProfileString("S", "K", "", sb, 100, _iniPath);
        Assert.Equal("second", sb.ToString());
    }

    [Fact]
    public void WritePrivateProfileString_CreatesFileAndDirectoryIfNotExist()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"IniTest_{Guid.NewGuid():N}");
        string path = Path.Combine(dir, "sub", "test.ini");
        try
        {
            bool result = IniFile.WritePrivateProfileString("S", "K", "v", path);
            Assert.True(result);
            Assert.True(File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void WritePrivateProfileString_MultipleSectionsAreIndependent()
    {
        IniFile.WritePrivateProfileString("Alpha", "Key", "alpha-value", _iniPath);
        IniFile.WritePrivateProfileString("Beta",  "Key", "beta-value",  _iniPath);

        var sb = new StringBuilder(100);
        IniFile.GetPrivateProfileString("Alpha", "Key", "", sb, 100, _iniPath);
        Assert.Equal("alpha-value", sb.ToString());

        IniFile.GetPrivateProfileString("Beta", "Key", "", sb, 100, _iniPath);
        Assert.Equal("beta-value", sb.ToString());
    }

    [Fact]
    public void WritePrivateProfileString_KeyLookupIsCaseInsensitive()
    {
        IniFile.WritePrivateProfileString("S", "MyKey", "val", _iniPath);
        var sb = new StringBuilder(100);
        IniFile.GetPrivateProfileString("S", "mykey", "", sb, 100, _iniPath);
        Assert.Equal("val", sb.ToString());
    }

    [Fact]
    public void WritePrivateProfileString_SectionLookupIsCaseInsensitive()
    {
        IniFile.WritePrivateProfileString("Section", "K", "v", _iniPath);
        var sb = new StringBuilder(100);
        IniFile.GetPrivateProfileString("SECTION", "K", "", sb, 100, _iniPath);
        Assert.Equal("v", sb.ToString());
    }

    [Fact]
    public void GetPrivateProfileString_IgnoresCommentLines()
    {
        File.WriteAllText(_iniPath, "[S]\n; comment\n# comment2\nKey=value\n");
        var sb = new StringBuilder(100);
        IniFile.GetPrivateProfileString("S", "Key", "", sb, 100, _iniPath);
        Assert.Equal("value", sb.ToString());
    }
}
