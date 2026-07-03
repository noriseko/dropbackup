using System.Collections.Generic;
using System.Windows;
using Res = DropBackup.Properties.Resources;

namespace DropBackup;

public partial class NotifyWindow : Window
{
    public NotifyWindow(int success, int failed, int skipped,
                        IReadOnlyList<string> skippedSymLinks,
                        IReadOnlyList<string> skippedByFilter)
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
    }

    private void btnClose_Click(object sender, RoutedEventArgs e) => Close();
}
