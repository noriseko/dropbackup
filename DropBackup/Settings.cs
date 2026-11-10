using System.IO;
using System.Linq;
using System.Text;

namespace DropBackup;

/// <summary>INI-backed configuration for the backup utility.</summary>
internal class Settings
{
    private readonly string? _iniPath;

    /// <param name="iniPath">Absolute path to the INI file. Pass <see langword="null"/> to use <see cref="GetIniFile"/>.</param>
    public Settings(string? iniPath = null)
    {
        _iniPath = iniPath;
    }

    // --- Backup settings ---
    /// <summary>Action to take when a backup name already exists.</summary>
    public OpeWhenExists Ope { get; set; } = OpeWhenExists.OpeRename;
    /// <summary>Position at which the timestamp is inserted into the file name.</summary>
    public AppendPos Pos { get; set; } = AppendPos.PosAfter;
    /// <summary>Timestamp format string (e.g. <c>yyMMdd-HHmm</c>).</summary>
    public string Fmt { get; set; } = "yyMMdd-HHmm";
    /// <summary>When <see langword="true"/>, symbolic links are excluded from backup.</summary>
    public bool ExcludeSymLinks { get; set; } = true;

    // --- Log settings ---
    /// <summary>When <see langword="true"/>, backup results are written to a log file.</summary>
    public bool EnableLog { get; set; } = false;
    /// <summary>Directory where daily log files are stored.</summary>
    public string LogDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DropBackup", "Logs");
    /// <summary>Number of days to keep log files (0 or negative disables pruning).</summary>
    public int LogRetentionDays { get; set; } = 30;

    // --- Filter settings ---
    /// <summary>File-name patterns to exclude (wildcards <c>*</c> and <c>?</c> are supported).</summary>
    public List<string> ExcludeFilters { get; set; } = new();

    // --- Destination settings ---
    /// <summary>When <see langword="true"/>, backups are written to <see cref="DestDir"/> instead of the source directory.</summary>
    public bool UseCustomDestDir { get; set; } = false;
    /// <summary>Target directory for backups. Effective only when <see cref="UseCustomDestDir"/> is <see langword="true"/>.</summary>
    public string DestDir { get; set; } = "";

    // --- Notification settings ---
    /// <summary>When <see langword="true"/>, toast notifications are used; otherwise a custom dialog is shown.</summary>
    public bool UseToast { get; set; } = false;

    /// <summary>When <see langword="true"/>, the completion dialog auto-closes after a short delay when there are no failures.</summary>
    public bool AutoCloseOnSuccess { get; set; } = false;

    public static string GetIniFile()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "DropBackup", "Settings.ini");
    }

    private string ResolveIniPath() => _iniPath ?? GetIniFile();

    public void Load()
    {
        string iniFile = ResolveIniPath();

        Ope = (OpeWhenExists)IniFile.GetPrivateProfileInt("Settings", "OpeWhenExists", (int)OpeWhenExists.OpeRename, iniFile);
        Pos = (AppendPos)IniFile.GetPrivateProfileInt("Settings", "AppendPos", (int)AppendPos.PosAfter, iniFile);

        StringBuilder sb = new StringBuilder(260);
        IniFile.GetPrivateProfileString("Settings", "Format", "yyMMdd-HHmm", sb, 260, iniFile);
        Fmt = sb.ToString();

        ExcludeSymLinks = IniFile.GetPrivateProfileInt("Settings", "ExcludeSymLinks", 1, iniFile) == 1;

        EnableLog = IniFile.GetPrivateProfileInt("Log", "EnableLog", 0, iniFile) == 1;

        StringBuilder sbDir = new StringBuilder(260);
        string defaultLogDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DropBackup", "Logs");
        IniFile.GetPrivateProfileString("Log", "LogDirectory", defaultLogDir, sbDir, 260, iniFile);
        LogDirectory = sbDir.ToString();

        LogRetentionDays = (int)IniFile.GetPrivateProfileInt("Log", "LogRetentionDays", 30, iniFile);

        StringBuilder sbFilters = new StringBuilder(4096);
        IniFile.GetPrivateProfileString("Filter", "Patterns", "", sbFilters, 4096, iniFile);
        string raw = sbFilters.ToString();
        ExcludeFilters = string.IsNullOrWhiteSpace(raw)
            ? new List<string>()
            : raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                 .ToList();

        UseCustomDestDir = IniFile.GetPrivateProfileInt("Destination", "UseCustomDestDir", 0, iniFile) == 1;
        StringBuilder sbDest = new StringBuilder(260);
        IniFile.GetPrivateProfileString("Destination", "DestDir", "", sbDest, 260, iniFile);
        DestDir = sbDest.ToString();

        UseToast = IniFile.GetPrivateProfileInt("Notification", "UseToast", 0, iniFile) == 1;
        AutoCloseOnSuccess = IniFile.GetPrivateProfileInt("Notification", "AutoCloseOnSuccess", 0, iniFile) == 1;
    }

    public void Save()
    {
        string iniFile = ResolveIniPath();

        IniFile.WritePrivateProfileString("Settings", "OpeWhenExists", ((int)Ope).ToString(), iniFile);
        IniFile.WritePrivateProfileString("Settings", "AppendPos", ((int)Pos).ToString(), iniFile);
        IniFile.WritePrivateProfileString("Settings", "Format", Fmt, iniFile);
        IniFile.WritePrivateProfileString("Settings", "ExcludeSymLinks", ExcludeSymLinks ? "1" : "0", iniFile);

        IniFile.WritePrivateProfileString("Log", "EnableLog", EnableLog ? "1" : "0", iniFile);
        IniFile.WritePrivateProfileString("Log", "LogDirectory", LogDirectory, iniFile);
        IniFile.WritePrivateProfileString("Log", "LogRetentionDays", LogRetentionDays.ToString(), iniFile);

        IniFile.WritePrivateProfileString("Filter", "Patterns",
            ExcludeFilters.Count > 0 ? string.Join(";", ExcludeFilters) : "", iniFile);

        IniFile.WritePrivateProfileString("Destination", "UseCustomDestDir", UseCustomDestDir ? "1" : "0", iniFile);
        IniFile.WritePrivateProfileString("Destination", "DestDir", DestDir, iniFile);

        IniFile.WritePrivateProfileString("Notification", "UseToast", UseToast ? "1" : "0", iniFile);
        IniFile.WritePrivateProfileString("Notification", "AutoCloseOnSuccess", AutoCloseOnSuccess ? "1" : "0", iniFile);
    }
}

