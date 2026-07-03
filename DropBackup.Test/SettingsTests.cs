using System.IO;
using DropBackup;
using Xunit;

namespace DropBackup.Test;

public class SettingsTests
{
    // -------------------------------------------------------------------
    // �f�t�H���g�l
    // -------------------------------------------------------------------

    [Fact]
    public void Default_Fmt_IsYyMMddHHmm()
    {
        var settings = new Settings();
        Assert.Equal("yyMMdd-HHmm", settings.Fmt);
    }

    [Fact]
    public void Default_Ope_IsOpeRename()
    {
        var settings = new Settings();
        Assert.Equal(OpeWhenExists.OpeRename, settings.Ope);
    }

    [Fact]
    public void Default_Pos_IsPosAfter()
    {
        var settings = new Settings();
        Assert.Equal(AppendPos.PosAfter, settings.Pos);
    }

    [Fact]
    public void Default_ExcludeSymLinks_IsTrue()
    {
        var settings = new Settings();
        Assert.True(settings.ExcludeSymLinks);
    }

    // -------------------------------------------------------------------
    // GetIniFile
    // -------------------------------------------------------------------

    [Fact]
    public void GetIniFile_ReturnsPathEndingWithSettingsIni()
    {
        string iniPath = Settings.GetIniFile();
        Assert.EndsWith("Settings.ini", iniPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetIniFile_DirectoryIsDropBackupUnderAppData()
    {
        string iniPath = Settings.GetIniFile();
        string expectedDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DropBackup");
        string actualDir = Path.GetDirectoryName(iniPath)!;
        Assert.Equal(expectedDir, actualDir, StringComparer.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------
    // Save / Load �̃��E���h�g���b�v�e�X�g�i�e���v INI ��g�p�j
    // -------------------------------------------------------------------

    [Fact]
    public void SaveAndLoad_Roundtrip_RestoresAllValues()
    {
        string iniPath = Path.Combine(Path.GetTempPath(), $"SettingsTest_{Guid.NewGuid():N}.ini");
        try
        {
            var s = new Settings(iniPath);
            s.Ope = OpeWhenExists.OpeOverride;
            s.Pos = AppendPos.PosBefore;
            s.Fmt = "yyyy-MM-dd";
            s.ExcludeSymLinks = false;
            s.Save();

            var s2 = new Settings(iniPath);
            s2.Load();

            Assert.Equal(OpeWhenExists.OpeOverride, s2.Ope);
            Assert.Equal(AppendPos.PosBefore, s2.Pos);
            Assert.Equal("yyyy-MM-dd", s2.Fmt);
            Assert.False(s2.ExcludeSymLinks);
        }
        finally
        {
            if (File.Exists(iniPath)) File.Delete(iniPath);
        }
    }

    [Fact]
    public void Load_WhenIniMissing_UsesDefaults()
    {
        string iniPath = Path.Combine(Path.GetTempPath(), $"SettingsTest_{Guid.NewGuid():N}.ini");
        var s = new Settings(iniPath);
        s.Load();

        Assert.Equal(OpeWhenExists.OpeRename, s.Ope);
        Assert.Equal(AppendPos.PosAfter, s.Pos);
        Assert.Equal("yyMMdd-HHmm", s.Fmt);
        Assert.True(s.ExcludeSymLinks);
    }

    [Fact]
    public void SaveAndLoad_Roundtrip_AllOpeValues()
    {
        string iniPath = Path.Combine(Path.GetTempPath(), $"SettingsTest_{Guid.NewGuid():N}.ini");
        try
        {
            foreach (OpeWhenExists ope in Enum.GetValues<OpeWhenExists>())
            {
                var s = new Settings(iniPath);
                s.Ope = ope;
                s.Save();

                var s2 = new Settings(iniPath);
                s2.Load();
                Assert.Equal(ope, s2.Ope);
            }
        }
        finally
        {
            if (File.Exists(iniPath)) File.Delete(iniPath);
        }
    }

    [Fact]
    public void SaveAndLoad_Roundtrip_AllPosValues()
    {
        string iniPath = Path.Combine(Path.GetTempPath(), $"SettingsTest_{Guid.NewGuid():N}.ini");
        try
        {
            foreach (AppendPos pos in Enum.GetValues<AppendPos>())
            {
                var s = new Settings(iniPath);
                s.Pos = pos;
                s.Save();

                var s2 = new Settings(iniPath);
                s2.Load();
                Assert.Equal(pos, s2.Pos);
            }
        }
        finally
        {
            if (File.Exists(iniPath)) File.Delete(iniPath);
        }
    }

    [Fact]
    public void Default_UseToast_IsFalse()
    {
        var s = new Settings();
        Assert.False(s.UseToast);
    }

    [Fact]
    public void SaveAndLoad_Roundtrip_UseToast()
    {
        string iniPath = Path.Combine(Path.GetTempPath(), $"SettingsTest_{Guid.NewGuid():N}.ini");
        try
        {
            var s = new Settings(iniPath);
            s.UseToast = true;
            s.Save();

            var s2 = new Settings(iniPath);
            s2.Load();
            Assert.True(s2.UseToast);
        }
        finally
        {
            if (File.Exists(iniPath)) File.Delete(iniPath);
        }
    }

    [Fact]
    public void SaveAndLoad_Roundtrip_LogRetentionDays_Zero()
    {
        string iniPath = Path.Combine(Path.GetTempPath(), $"SettingsTest_{Guid.NewGuid():N}.ini");
        try
        {
            var s = new Settings(iniPath);
            s.LogRetentionDays = 0;
            s.Save();

            var s2 = new Settings(iniPath);
            s2.Load();
            Assert.Equal(0, s2.LogRetentionDays);
        }
        finally
        {
            if (File.Exists(iniPath)) File.Delete(iniPath);
        }
    }
}
