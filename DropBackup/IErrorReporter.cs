namespace DropBackup;

internal interface IErrorReporter
{
    /// <summary>
    /// Reports an error to the user.
    /// Returns <see langword="true"/> to continue processing, or <see langword="false"/> to abort.
    /// </summary>
    bool Report(string message, string? title = null);
}
