namespace DropBackup;

/// <summary>Action to take when a backup with the same name already exists.</summary>
internal enum OpeWhenExists
{
    /// <summary>Append a sequential number to the backup name.</summary>
    OpeRename,
    /// <summary>Overwrite the existing backup without deleting it first.</summary>
    OpeOverride,
    /// <summary>Delete the existing backup, then create a new one.</summary>
    OpeDelete,
}
