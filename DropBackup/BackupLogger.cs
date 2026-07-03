using System;
using System.IO;
using System.Text;

namespace DropBackup;

/// <summary>Appends backup results to a daily log file and prunes old logs.</summary>
internal class BackupLogger
{
    private readonly string _logDirectory;
    private readonly int _retentionDays;

    /// <summary>Initializes a new instance of <see cref="BackupLogger"/>.</summary>
    /// <param name="logDirectory">Directory in which log files are stored.</param>
    /// <param name="retentionDays">
    /// Number of days to retain log files.  Files older than this threshold are deleted
    /// on each write.  Pass 0 or a negative value to disable deletion.
    /// </param>
    public BackupLogger(string logDirectory, int retentionDays)
    {
        _logDirectory = logDirectory;
        _retentionDays = retentionDays;
    }

    /// <summary>Appends a summary entry to today's log file, then prunes old logs.</summary>
    /// <param name="success">Number of successfully backed-up items.</param>
    /// <param name="failed">Number of items that failed.</param>
    /// <param name="skipped">Number of skipped items.</param>
    /// <param name="targets">Paths that were processed.</param>
    /// <param name="skippedSymLinks">Symbolic-link paths that were skipped.</param>
    public void WriteLog(int success, int failed, int skipped,
        IReadOnlyList<string> targets, IReadOnlyList<string> skippedSymLinks)
    {
        Directory.CreateDirectory(_logDirectory);

        string logFile = Path.Combine(_logDirectory, $"backup_{DateTime.Now:yyyyMMdd}.log");
        var sb = new StringBuilder();
        sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]  success:{success}  failed:{failed}  skipped:{skipped}");
        foreach (string t in targets)
            sb.AppendLine($"  target:       {t}");
        foreach (string s in skippedSymLinks)
            sb.AppendLine($"  skipped-link: {s}");

        File.AppendAllText(logFile, sb.ToString(), Encoding.UTF8);

        Rotate();
    }

    /// <summary>Deletes log files older than <see cref="_retentionDays"/> days.</summary>
    public void Rotate()
    {
        if (_retentionDays <= 0) return;
        if (!Directory.Exists(_logDirectory)) return;

        DateTime cutoff = DateTime.Now.Date.AddDays(-_retentionDays);
        foreach (string file in Directory.GetFiles(_logDirectory, "backup_*.log"))
        {
            // Expected file name format: backup_yyyyMMdd.log
            string name = Path.GetFileNameWithoutExtension(file);
            if (name.Length == 15 &&
                DateTime.TryParseExact(name[7..], "yyyyMMdd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime fileDate)
                && fileDate < cutoff)
            {
                File.Delete(file);
            }
        }
    }
}
