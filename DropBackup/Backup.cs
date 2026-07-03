using System;
using System.IO;
using System.IO.Enumeration;
using Res = DropBackup.Properties.Resources;

namespace DropBackup;

internal class Backup
{
    private readonly OpeWhenExists _ope;
    private readonly AppendPos _pos;
    private readonly string _fmt;
    private readonly bool _excludeSymLinks;
    private readonly IReadOnlyList<string> _excludeFilters;
    private readonly string _destDir; // empty string means same directory as the source
    private readonly IErrorReporter _errorReporter;

    /// <summary>Paths skipped because they are symbolic links and <c>ExcludeSymLinks</c> is enabled.</summary>
    public List<string> SkippedSymLinks { get; } = new();

    /// <summary>Paths skipped because they matched one of the exclude filter patterns.</summary>
    public List<string> SkippedByFilter { get; } = new();

    private static bool IsSymbolicLink(string path)
    {
        // File.GetAttributes throws if path does not exist
        if (!File.Exists(path) && !Directory.Exists(path))
            return false;
        FileAttributes attr = File.GetAttributes(path);
        return (attr & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
    }

    private bool IsExcludedByFilter(string path)
    {
        if (_excludeFilters.Count == 0) return false;
        string name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(name)) name = path;
        foreach (string pattern in _excludeFilters)
        {
            if (FileSystemName.MatchesSimpleExpression(pattern, name, ignoreCase: true))
                return true;
        }
        return false;
    }

    public Backup(Settings settings, IErrorReporter? errorReporter = null)
    {
        _ope = settings.Ope;
        _pos = settings.Pos;
        _fmt = settings.Fmt;
        _excludeSymLinks = settings.ExcludeSymLinks;
        _excludeFilters = settings.ExcludeFilters;
        _destDir = settings.UseCustomDestDir ? settings.DestDir : "";
        _errorReporter = errorReporter ?? new MessageBoxErrorReporter();
    }

    internal Backup(OpeWhenExists ope, AppendPos pos, string fmt, bool excludeSymLinks, IErrorReporter errorReporter,
        IReadOnlyList<string>? excludeFilters = null, string destDir = "")
    {
        _ope = ope;
        _pos = pos;
        _fmt = fmt;
        _excludeSymLinks = excludeSymLinks;
        _excludeFilters = excludeFilters ?? Array.Empty<string>();
        _destDir = destDir;
        _errorReporter = errorReporter;
    }

    public BackupStatus Execute(string path)
    {
        if (_excludeSymLinks && IsSymbolicLink(path))
        {
            SkippedSymLinks.Add(path);
            return BackupStatus.Skipped;
        }

        if (IsExcludedByFilter(path))
        {
            SkippedByFilter.Add(path);
            return BackupStatus.Skipped;
        }

        // null = error but user chose to continue, false = error and user aborted, true = success
        bool? result;
        if (Directory.Exists(path))
            result = BackupDirectory(path);
        else if (File.Exists(path))
            result = BackupFile(path);
        else
            return BackupStatus.Failed;

        return result switch
        {
            true  => BackupStatus.Success,
            null  => BackupStatus.Failed,
            false => BackupStatus.Aborted,
        };
    }

    // true = success, false = aborted, null = error but continue (Failed)
    private bool? BackupDirectory(string dirName)
    {
        if (!ResolveBackName(dirName, out string backName, out bool aborted))
            return aborted ? false : null;
        return CopyDirectory(dirName, backName);
    }

    private bool? BackupFile(string fileName)
    {
        if (!ResolveBackName(fileName, out string backName, out bool aborted))
            return aborted ? false : null;
        return CopyFile(fileName, backName);
    }

    // Resolves the destination backup path, applying OpeWhenExists logic.
    // Returns false if the backup name could not be resolved.
    // aborted=true: user chose to abort; aborted=false: could not resolve (sequence limit reached, etc.)
    private bool ResolveBackName(string sourcePath, out string backName, out bool aborted)
    {
        aborted = false;
        backName = GetTimestampedName(sourcePath);
        if (File.Exists(backName) || Directory.Exists(backName))
        {
            switch (_ope)
            {
                case OpeWhenExists.OpeDelete:
                    if (!Delete(backName, out aborted))
                        return false;
                    break;
                case OpeWhenExists.OpeRename:
                    backName = GetTimestampedNameWithSequence(sourcePath);
                    if (backName == "")
                        return false;
                    break;
            }
        }
        return true;
    }

    private bool Delete(string path, out bool aborted)
    {
        aborted = false;
        bool? result;
        if (Directory.Exists(path))
            result = DeleteDirectory(path);
        else if (File.Exists(path))
            result = DeleteFile(path);
        else
            return false;
        aborted = result == false;
        return result == true;
    }

    private bool? DeleteDirectory(string dirName)
    {
        foreach (string fileName in Directory.GetFiles(dirName))
        {
            var r = DeleteFile(fileName);
            if (r != true) return r;
        }
        foreach (string subDir in Directory.GetDirectories(dirName))
        {
            var r = DeleteDirectory(subDir);
            if (r != true) return r;
        }
        try
        {
            Directory.Delete(dirName);
        }
        catch (Exception ex)
        {
            return _errorReporter.Report(ex.Message) ? null : false;
        }
        return true;
    }

    private bool? DeleteFile(string fileName)
    {
        try
        {
            File.Delete(fileName);
        }
        catch (Exception ex)
        {
            // true = continue (treated as Failed Å® null), false = abort (Aborted Å® false)
            return _errorReporter.Report(ex.Message, "Error") ? null : false;
        }
        return true;
    }

    private bool? CopyDirectory(string srcDir, string dstDir)
    {
        if (_excludeSymLinks && IsSymbolicLink(srcDir))
        {
            SkippedSymLinks.Add(srcDir);
            return true;
        }
        try
        {
            if (!Directory.Exists(dstDir))
                Directory.CreateDirectory(dstDir);
        }
        catch (Exception ex)
        {
            return _errorReporter.Report(ex.Message, "Error") ? null : false;
        }
        foreach (string srcFile in Directory.GetFiles(srcDir))
        {
            string fileName = Path.GetFileName(srcFile);
            string dstFile = Path.Combine(dstDir, fileName);
            var r = CopyFile(srcFile, dstFile);
            if (r != true) return r;
        }
        foreach (string srcSubDir in Directory.GetDirectories(srcDir))
        {
            string dirName = Path.GetFileName(srcSubDir);
            string dstSubDir = Path.Combine(dstDir, dirName);
            var r = CopyDirectory(srcSubDir, dstSubDir);
            if (r != true) return r;
        }
        return true;
    }

    private bool? CopyFile(string srcFile, string dstFile)
    {
        if (_excludeSymLinks && IsSymbolicLink(srcFile))
        {
            SkippedSymLinks.Add(srcFile);
            return true;
        }
        try
        {
            File.Copy(srcFile, dstFile, true);
        }
        catch (Exception ex)
        {
            return _errorReporter.Report(ex.Message, "Error") ? null : false;
        }
        return true;
    }

    private string FormatTitle(string title)
    {
        DateTime now = DateTime.Now;
        string str = now.ToString(_fmt);
        if (_pos == AppendPos.PosAfter)
        {
            return title + " " + str;
        }
        else
        {
            return str + " " + title;
        }
    }

    private string GetTimestampedName(string path)
    {
        string? dir = string.IsNullOrEmpty(_destDir) ? Path.GetDirectoryName(path) : _destDir;
        string title = Path.GetFileNameWithoutExtension(path);
        string ext = Path.GetExtension(path);
        title = FormatTitle(title);
        return Path.Combine(dir ?? "", title + ext);
    }

    private string GetTimestampedNameWithSequence(string path)
    {
        string? dir = string.IsNullOrEmpty(_destDir) ? Path.GetDirectoryName(path) : _destDir;
        string title = Path.GetFileNameWithoutExtension(path);
        string ext = Path.GetExtension(path);
        title = FormatTitle(title);
        for (int i = 1; i < 99; i++)
        {
            string num = string.Format("-{0}", i);
            string str = Path.Combine(dir ?? "", title + num + ext);
            if (!File.Exists(str) && !Directory.Exists(str))
            {
                return str;
            }
        }
        _errorReporter.Report(string.Format(Res.Err_SequentialNumberLimit, path), "Error");
        return ""; // skip this item even if the user chose to continue
    }
}
