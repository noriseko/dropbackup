namespace DropBackup;

/// <summary>Position at which the timestamp is inserted into the file name.</summary>
internal enum AppendPos
{
    /// <summary>Timestamp is prepended before the original file name.</summary>
    PosBefore,
    /// <summary>Timestamp is appended after the original file name.</summary>
    PosAfter,
}
