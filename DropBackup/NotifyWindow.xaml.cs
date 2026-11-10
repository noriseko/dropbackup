using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using Res = DropBackup.Properties.Resources;

namespace DropBackup;

public partial class NotifyWindow : Window
{
    private static readonly System.TimeSpan AutoCloseDelay = System.TimeSpan.FromSeconds(5);
    private DispatcherTimer? _autoCloseTimer;

    public NotifyWindow(int success, int failed, int skipped,
                        IReadOnlyList<string> skippedSymLinks,
                        IReadOnlyList<string> skippedByFilter,
                        bool autoCloseOnSuccess = false)
    {
        InitializeComponent();

        // Counters
        tbSuccessCount.Text = success.ToString();
        tbSuccessLabel.Text = Res.Notify_LabelSuccess;

        tbFailedCount.Text  = failed.ToString();
        tbFailedLabel.Text  = Res.Notify_LabelFailed;

        tbSkippedCount.Text = skipped.ToString();
        tbSkippedLabel.Text = Res.Notify_LabelSkipped;

        // Highlight failed count in red if > 0
        if (failed == 0)
        {
            tbFailedCount.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xBB, 0xBB, 0xBB));
        }

        // Detail sections
        bool hasDetails = false;

        if (skippedSymLinks.Count > 0)
        {
            tbSymLinksHeader.Text = Res.Notify_SectionSymLinks;
            tbSymLinks.Text       = string.Join("\n", skippedSymLinks);
            pnlSymLinks.Visibility = Visibility.Visible;
            hasDetails = true;
        }

        if (skippedByFilter.Count > 0)
        {
            tbFilterHeader.Text = Res.Notify_SectionFilter;
            tbFilter.Text       = string.Join("\n", skippedByFilter);
            pnlFilter.Visibility = Visibility.Visible;
            hasDetails = true;
        }

        if (hasDetails)
            pnlDetails.Visibility = Visibility.Visible;

        // Button label
        btnClose.Content = Res.Btn_Close;

        // Auto-close after a short delay when there are no failures and no details to review.
        if (autoCloseOnSuccess && failed == 0 && !hasDetails)
        {
            _autoCloseTimer = new DispatcherTimer { Interval = AutoCloseDelay };
            _autoCloseTimer.Tick += (_, _) =>
            {
                _autoCloseTimer!.Stop();
                Close();
            };
            Loaded += (_, _) => _autoCloseTimer.Start();
        }
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        _autoCloseTimer?.Stop();
        Close();
    }
}
