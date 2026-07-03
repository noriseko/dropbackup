using System;
using System.IO;
using DropBackup;
using Xunit;

namespace DropBackup.Test;

public class BackupDestDirTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FakeErrorReporter _reporter;

    public BackupDestDirTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "DuplicateDestDirTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _reporter = new FakeErrorReporter();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private Backup CreateBackup(string destDir)
        => new Backup(OpeWhenExists.OpeRename, AppendPos.PosAfter, "'BAK'", false, _reporter, destDir: destDir);

    private Backup CreateBackupSameDir()
        => new Backup(OpeWhenExists.OpeRename, AppendPos.PosAfter, "'BAK'", false, _reporter);

    private string CreateTempFile(string subDir, string name, string content = "data")
    {
        string dir = Path.Combine(_tempDir, subDir);
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, name);
        File.WriteAllText(path, content);
        return path;
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void NoCustomDest_BackupCreatedInSameDir()
    {
        string src = CreateTempFile("src", "hello.txt");
        var backup = CreateBackupSameDir();

        var status = backup.Execute(src);

        Assert.Equal(BackupStatus.Success, status);
        string[] files = Directory.GetFiles(Path.Combine(_tempDir, "src"));
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void CustomDest_BackupCreatedInDestDir()
    {
        string destDir = Path.Combine(_tempDir, "dest");
        Directory.CreateDirectory(destDir);

        string src = CreateTempFile("src", "hello.txt");
        var backup = CreateBackup(destDir);

        var status = backup.Execute(src);

        Assert.Equal(BackupStatus.Success, status);
        Assert.Single(Directory.GetFiles(Path.Combine(_tempDir, "src")));
        Assert.Single(Directory.GetFiles(destDir));
    }

    [Fact]
    public void CustomDest_BackupFileNameContainsTimestamp()
    {
        string destDir = Path.Combine(_tempDir, "dest");
        Directory.CreateDirectory(destDir);

        string src = CreateTempFile("src", "report.txt");
        var backup = CreateBackup(destDir);

        backup.Execute(src);

        string[] created = Directory.GetFiles(destDir);
        Assert.Single(created);
        Assert.Contains("BAK", Path.GetFileName(created[0]));
    }

    [Fact]
    public void CustomDest_DirectoryBackup_CopiedToDestDir()
    {
        string destDir = Path.Combine(_tempDir, "dest");
        Directory.CreateDirectory(destDir);

        string srcDir = Path.Combine(_tempDir, "srcfolder");
        Directory.CreateDirectory(srcDir);
        File.WriteAllText(Path.Combine(srcDir, "a.txt"), "aaa");

        var backup = CreateBackup(destDir);

        var status = backup.Execute(srcDir);

        Assert.Equal(BackupStatus.Success, status);
        string[] destDirs = Directory.GetDirectories(destDir);
        Assert.Single(destDirs);
    }

    [Fact]
    public void CustomDest_RenameConflict_SequenceSuffixedInDestDir()
    {
        string destDir = Path.Combine(_tempDir, "dest");
        Directory.CreateDirectory(destDir);

        string src = CreateTempFile("src", "file.txt");
        var backup = CreateBackup(destDir);

        backup.Execute(src);
        var status2 = backup.Execute(src);

        Assert.Equal(BackupStatus.Success, status2);
        Assert.Equal(2, Directory.GetFiles(destDir).Length);
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void Settings_UseCustomDestDir_RoundTrip()
    {
        string iniFile = Path.Combine(_tempDir, "test.ini");
        var s = new Settings(iniFile);
        s.UseCustomDestDir = true;
        s.DestDir = @"C:\Backup";
        s.Save();

        var s2 = new Settings(iniFile);
        s2.Load();

        Assert.True(s2.UseCustomDestDir);
        Assert.Equal(@"C:\Backup", s2.DestDir);
    }

    [Fact]
    public void Settings_UseCustomDestDir_DefaultFalse()
    {
        string iniFile = Path.Combine(_tempDir, "empty.ini");
        var s = new Settings(iniFile);
        s.Load();

        Assert.False(s.UseCustomDestDir);
        Assert.Equal("", s.DestDir);
    }

    [Fact]
    public void Settings_UseCustomDestDir_FalseRoundTrip()
    {
        string iniFile = Path.Combine(_tempDir, "test2.ini");
        var s = new Settings(iniFile);
        s.UseCustomDestDir = false;
        s.DestDir = "";
        s.Save();

        var s2 = new Settings(iniFile);
        s2.Load();

        Assert.False(s2.UseCustomDestDir);
    }
}
