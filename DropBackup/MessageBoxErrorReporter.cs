using System.Windows;

namespace DropBackup;

internal class MessageBoxErrorReporter : IErrorReporter
{
    public bool Report(string message, string? title = null)
    {
        string caption = title ?? "Error";
        string text = message + "\n\nDo you want to continue?";
        var result = MessageBox.Show(text, caption, MessageBoxButton.YesNo, MessageBoxImage.Warning);
        return result == MessageBoxResult.Yes;
    }
}
