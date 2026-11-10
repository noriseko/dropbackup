using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using Res = DropBackup.Properties.Resources;

namespace DropBackup;

public partial class NotifyWindow : Window
{
    private static readonly System.TimeSpan AutoCloseDelay = System.TimeSpan.FromSeconds(5);
    private DispatcherTimer? _autoCloseTimer;
    private int _remainingSeconds;

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
            _remainingSeconds = (int)AutoCloseDelay.TotalSeconds;
            btnClose.Content = string.Format(Res.Btn_CloseCountdown, _remainingSeconds);

            _autoCloseTimer = new DispatcherTimer { Interval = System.TimeSpan.FromSeconds(1) };
            _autoCloseTimer.Tick += (_, _) =>
            {
                _remainingSeconds--;
                if (_remainingSeconds <= 0)
                {
                    _autoCloseTimer!.Stop();
                    Close();
                }
                else
                {
                    btnClose.Content = string.Format(Res.Btn_CloseCountdown, _remainingSeconds);
                }
            };
            Loaded += (_, _) => _autoCloseTimer.Start();

            // Clicking anywhere on the window cancels the countdown (but not via the Close button, which closes).
            PreviewMouseDown += NotifyWindow_PreviewMouseDown;
        }
    }

    private void NotifyWindow_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Let the Close button perform its normal close action.
        if (IsClickOnCloseButton(e.OriginalSource as DependencyObject))
            return;

        CancelAutoClose();
    }

    private bool IsClickOnCloseButton(DependencyObject? source)
    {
        while (source != null)
        {
            if (ReferenceEquals(source, btnClose))
                return true;
            source = System.Windows.Media.VisualTreeHelper.GetParent(source);
        }
        return false;
    }

    private void CancelAutoClose()
    {
        if (_autoCloseTimer == null)
            return;

        _autoCloseTimer.Stop();
        _autoCloseTimer = null;
        PreviewMouseDown -= NotifyWindow_PreviewMouseDown;
        btnClose.Content = Res.Btn_Close;
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        _autoCloseTimer?.Stop();
        Close();
    }
}
