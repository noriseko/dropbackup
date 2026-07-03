using DropBackup;

namespace DropBackup.Test;

internal class FakeErrorReporter : IErrorReporter
{
    public List<(string Message, string? Title)> Reports { get; } = new();

    public bool ContinueOnError { get; set; } = false;

    public bool Report(string message, string? title = null)
    {
        Reports.Add((message, title));
        return ContinueOnError;
    }
}
