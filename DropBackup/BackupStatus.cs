namespace DropBackup;

/// <summary>Result of a single backup operation.</summary>
internal enum BackupStatus
{
    /// <summary>The item was backed up successfully.</summary>
    Success,
    /// <summary>The item was skipped (symbolic link or filter exclusion).</summary>
    Skipped,
    /// <summary>An error occurred but the user chose to continue.</summary>
    Failed,
    /// <summary>An error occurred and the user chose to abort further processing.</summary>
    Aborted,
}
