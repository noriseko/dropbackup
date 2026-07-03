using System;
using System.IO;
using DropBackup;
using Xunit;

namespace DropBackup.Test;

public class BackupFilterTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FakeErrorReporter _reporter;

    public BackupFilterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "DuplicateFilterTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _reporter = new FakeErrorReporter();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private Backup CreateBackup(params string[] filters)
        => new Backup(OpeWhenExists.OpeRename, AppendPos.PosAfter, "'BAK'", false, _reporter, filters);

    private string CreateTempFile(string name, string content = "data")
    {
        string path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, content);
        return path;
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void NoFilters_FileIsBackedUp()
    {
        string src = CreateTempFile("test.tmp");
        var backup = CreateBackup();
        var result = backup.Execute(src);

        Assert.Equal(BackupStatus.Success, result);
        Assert.Empty(backup.SkippedByFilter);
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void Filter_WildcardExtension_SkipsMatchingFile()
    {
        string src = CreateTempFile("work.tmp");
        var backup = CreateBackup("*.tmp");
        var result = backup.Execute(src);

        Assert.Equal(BackupStatus.Skipped, result);
        Assert.Contains(src, backup.SkippedByFilter);
    }

    [Fact]
    public void Filter_WildcardExtension_DoesNotSkipNonMatchingFile()
    {
        string src = CreateTempFile("work.txt");
        var backup = CreateBackup("*.tmp");
        var result = backup.Execute(src);

        Assert.Equal(BackupStatus.Success, result);
        Assert.Empty(backup.SkippedByFilter);
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void Filter_ExactName_SkipsMatchingFile()
    {
        string src = CreateTempFile("Thumbs.db");
        var backup = CreateBackup("Thumbs.db");
        var result = backup.Execute(src);

        Assert.Equal(BackupStatus.Skipped, result);
        Assert.Contains(src, backup.SkippedByFilter);
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void Filter_IsCaseInsensitive()
    {
        string src = CreateTempFile("THUMBS.DB");
        var backup = CreateBackup("thumbs.db");
        var result = backup.Execute(src);

        Assert.Equal(BackupStatus.Skipped, result);
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void Filter_QuestionMark_MatchesSingleChar()
    {
        string src = CreateTempFile("log1.bak");
        var backup = CreateBackup("log?.bak");
        var result = backup.Execute(src);

        Assert.Equal(BackupStatus.Skipped, result);
    }

    [Fact]
    public void Filter_QuestionMark_DoesNotMatchMultipleChars()
    {
        string src = CreateTempFile("log123.bak");
        var backup = CreateBackup("log?.bak");
        var result = backup.Execute(src);

        Assert.Equal(BackupStatus.Success, result);
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void Filter_MultiplePatterns_SkipsIfAnyMatches()
    {
        string src1 = CreateTempFile("a.tmp");
        string src2 = CreateTempFile("Thumbs.db");
        string src3 = CreateTempFile("keep.txt");

        var backup = CreateBackup("*.tmp", "Thumbs.db");

        Assert.Equal(BackupStatus.Skipped, backup.Execute(src1));
        Assert.Equal(BackupStatus.Skipped, backup.Execute(src2));
        Assert.Equal(BackupStatus.Success,  backup.Execute(src3));
        Assert.Equal(2, backup.SkippedByFilter.Count);
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void Filter_TildeWildcard_SkipsTempOfficeFiles()
    {
        string src = CreateTempFile("~$document.docx");
        var backup = CreateBackup("~*");
        var result = backup.Execute(src);

        Assert.Equal(BackupStatus.Skipped, result);
    }

    [Fact]
    public void Filter_MatchesDirectory_SkipsDirectory()
    {
        // �t�B���^�p�^�[�����f�B���N�g�����Ɉ�v����Ƃ��A���̃f�B���N�g����X�L�b�v����
        string srcDir = Path.Combine(_tempDir, "node_modules");
        Directory.CreateDirectory(srcDir);
        File.WriteAllText(Path.Combine(srcDir, "pkg.js"), "data");

        var backup = CreateBackup("node_modules");
        var result = backup.Execute(srcDir);

        Assert.Equal(BackupStatus.Skipped, result);
        Assert.Contains(srcDir, backup.SkippedByFilter);
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void Settings_ExcludeFilters_DefaultIsEmpty()
    {
        var s = new Settings();
        Assert.Empty(s.ExcludeFilters);
    }

    [Fact]
    public void Settings_ExcludeFilters_Roundtrip()
    {
        string iniPath = Path.Combine(Path.GetTempPath(), $"FilterTest_{Guid.NewGuid():N}.ini");
        try
        {
            var s = new Settings(iniPath);
            s.ExcludeFilters = new System.Collections.Generic.List<string> { "*.tmp", "Thumbs.db", "~*" };
            s.Save();

            var s2 = new Settings(iniPath);
            s2.Load();

            Assert.Equal(3, s2.ExcludeFilters.Count);
            Assert.Contains("*.tmp", s2.ExcludeFilters);
            Assert.Contains("Thumbs.db", s2.ExcludeFilters);
            Assert.Contains("~*", s2.ExcludeFilters);
        }
        finally
        {
            if (File.Exists(iniPath)) File.Delete(iniPath);
        }
    }

    [Fact]
    public void Settings_ExcludeFilters_EmptyListSavesAndLoadsAsEmpty()
    {
        string iniPath = Path.Combine(Path.GetTempPath(), $"FilterTest_{Guid.NewGuid():N}.ini");
        try
        {
            var s = new Settings(iniPath);
            s.ExcludeFilters = new System.Collections.Generic.List<string>();
            s.Save();

            var s2 = new Settings(iniPath);
            s2.Load();
            Assert.Empty(s2.ExcludeFilters);
        }
        finally
        {
            if (File.Exists(iniPath)) File.Delete(iniPath);
        }
    }
}
