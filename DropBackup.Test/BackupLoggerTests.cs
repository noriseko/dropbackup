using System;
using System.IO;
using System.Linq;
using DropBackup;
using Xunit;

namespace DropBackup.Test;

public class BackupLoggerTests : IDisposable
{
    private readonly string _logDir;

    public BackupLoggerTests()
    {
        _logDir = Path.Combine(Path.GetTempPath(), "DuplicateLogTest_" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_logDir))
            Directory.Delete(_logDir, recursive: true);
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void WriteLog_CreatesLogFileInDirectory()
    {
        var logger = new BackupLogger(_logDir, 30);
        logger.WriteLog(1, 0, 0, new[] { @"C:\foo.txt" }, Array.Empty<string>());

        Assert.True(Directory.Exists(_logDir));
        Assert.Single(Directory.GetFiles(_logDir, "backup_*.log"));
    }

    [Fact]
    public void WriteLog_FileNameContainsTodaysDate()
    {
        var logger = new BackupLogger(_logDir, 30);
        logger.WriteLog(1, 0, 0, new[] { @"C:\foo.txt" }, Array.Empty<string>());

        string expected = $"backup_{DateTime.Now:yyyyMMdd}.log";
        Assert.True(File.Exists(Path.Combine(_logDir, expected)));
    }

    [Fact]
    public void WriteLog_ContainsSuccessFailedSkippedCounts()
    {
        var logger = new BackupLogger(_logDir, 30);
        logger.WriteLog(3, 1, 2, new[] { @"C:\foo.txt" }, Array.Empty<string>());

        string content = File.ReadAllText(Path.Combine(_logDir, $"backup_{DateTime.Now:yyyyMMdd}.log"));
        Assert.Contains("success:3", content);
        Assert.Contains("failed:1", content);
        Assert.Contains("skipped:2", content);
    }

    [Fact]
    public void WriteLog_ContainsTargetPaths()
    {
        var logger = new BackupLogger(_logDir, 30);
        logger.WriteLog(1, 0, 0, new[] { @"C:\a.txt", @"C:\b.txt" }, Array.Empty<string>());

        string content = File.ReadAllText(Path.Combine(_logDir, $"backup_{DateTime.Now:yyyyMMdd}.log"));
        Assert.Contains(@"C:\a.txt", content);
        Assert.Contains(@"C:\b.txt", content);
    }

    [Fact]
    public void WriteLog_ContainsSkippedSymLinks()
    {
        var logger = new BackupLogger(_logDir, 30);
        logger.WriteLog(0, 0, 1, Array.Empty<string>(), new[] { @"C:\link" });

        string content = File.ReadAllText(Path.Combine(_logDir, $"backup_{DateTime.Now:yyyyMMdd}.log"));
        Assert.Contains(@"C:\link", content);
    }

    [Fact]
    public void WriteLog_AppendsToExistingFile()
    {
        var logger = new BackupLogger(_logDir, 30);
        logger.WriteLog(1, 0, 0, new[] { @"C:\a.txt" }, Array.Empty<string>());
        logger.WriteLog(2, 0, 0, new[] { @"C:\b.txt" }, Array.Empty<string>());

        string logFile = Path.Combine(_logDir, $"backup_{DateTime.Now:yyyyMMdd}.log");
        string content = File.ReadAllText(logFile);
        Assert.Contains(@"C:\a.txt", content);
        Assert.Contains(@"C:\b.txt", content);
    }

    [Fact]
    public void WriteLog_CreatesDirectoryIfNotExist()
    {
        string nestedDir = Path.Combine(_logDir, "sub", "logs");
        var logger = new BackupLogger(nestedDir, 30);
        logger.WriteLog(1, 0, 0, Array.Empty<string>(), Array.Empty<string>());

        Assert.True(Directory.Exists(nestedDir));
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void Rotate_DeletesFilesOlderThanRetentionDays()
    {
        Directory.CreateDirectory(_logDir);
        string oldFile = Path.Combine(_logDir, "backup_20000101.log");
        File.WriteAllText(oldFile, "old");

        var logger = new BackupLogger(_logDir, 30);
        logger.Rotate();

        Assert.False(File.Exists(oldFile));
    }

    [Fact]
    public void Rotate_KeepsFilesWithinRetentionDays()
    {
        Directory.CreateDirectory(_logDir);
        string todayFile = Path.Combine(_logDir, $"backup_{DateTime.Now:yyyyMMdd}.log");
        File.WriteAllText(todayFile, "today");

        var logger = new BackupLogger(_logDir, 30);
        logger.Rotate();

        Assert.True(File.Exists(todayFile));
    }

    [Fact]
    public void Rotate_DoesNotThrowWhenDirectoryMissing()
    {
        var logger = new BackupLogger(Path.Combine(_logDir, "nonexistent"), 30);
        var ex = Record.Exception(() => logger.Rotate());
        Assert.Null(ex);
    }

    [Fact]
    public void Rotate_DoesNothingWhenRetentionDaysIsZero()
    {
        Directory.CreateDirectory(_logDir);
        string oldFile = Path.Combine(_logDir, "backup_20000101.log");
        File.WriteAllText(oldFile, "old");

        var logger = new BackupLogger(_logDir, 0);
        logger.Rotate();

        Assert.True(File.Exists(oldFile));
    }

    [Fact]
    public void Rotate_DoesNothingWhenRetentionDaysIsNegative()
    {
        Directory.CreateDirectory(_logDir);
        string oldFile = Path.Combine(_logDir, "backup_20000101.log");
        File.WriteAllText(oldFile, "old");

        var logger = new BackupLogger(_logDir, -1);
        logger.Rotate();

        Assert.True(File.Exists(oldFile));
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void Settings_LogDefaults()
    {
        var s = new Settings();
        Assert.False(s.EnableLog);
        Assert.EndsWith("Logs", s.LogDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(30, s.LogRetentionDays);
    }

    [Fact]
    public void Settings_LogRoundtrip()
    {
        string iniPath = Path.Combine(Path.GetTempPath(), $"SettingsLog_{Guid.NewGuid():N}.ini");
        try
        {
            var s = new Settings(iniPath);
            s.EnableLog = true;
            s.LogDirectory = @"C:\Logs\Backup";
            s.LogRetentionDays = 14;
            s.Save();

            var s2 = new Settings(iniPath);
            s2.Load();
            Assert.True(s2.EnableLog);
            Assert.Equal(@"C:\Logs\Backup", s2.LogDirectory);
            Assert.Equal(14, s2.LogRetentionDays);
        }
        finally
        {
            if (File.Exists(iniPath)) File.Delete(iniPath);
        }
    }
}
