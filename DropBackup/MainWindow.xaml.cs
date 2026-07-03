using System;
using System.IO;
using System.Linq;
using System.Windows;
using Res = DropBackup.Properties.Resources;

namespace DropBackup;

public partial class MainWindow : Window
{
    private readonly Settings _settings = new Settings();

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _settings.Load();

        // --- Backup tab ---
        tbFormat.Text = _settings.Fmt;

        switch (_settings.Pos)
        {
            case AppendPos.PosBefore: rbPosBefore.IsChecked = true; break;
            case AppendPos.PosAfter:  rbPosAfter.IsChecked  = true; break;
        }

        switch (_settings.Ope)
        {
            case OpeWhenExists.OpeRename:   rbOpeRename.IsChecked   = true; break;
            case OpeWhenExists.OpeOverride: rbOpeOverride.IsChecked = true; break;
            case OpeWhenExists.OpeDelete:   rbOpeDelete.IsChecked   = true; break;
        }

        cbExcludeSymLinks.IsChecked = _settings.ExcludeSymLinks;

        // --- Exclude filters tab ---
        lbFilters.Items.Clear();
        foreach (string p in _settings.ExcludeFilters)
            lbFilters.Items.Add(p);

        // --- Destination tab ---
        if (_settings.UseCustomDestDir)
        {
            rbDestCustomDir.IsChecked = true;
            pnlDestDir.IsEnabled = true;
        }
        else
        {
            rbDestSameDir.IsChecked = true;
            pnlDestDir.IsEnabled = false;
        }
        tbDestDir.Text = _settings.DestDir;

        // --- Log tab ---
        cbEnableLog.IsChecked   = _settings.EnableLog;
        tbLogDirectory.Text     = _settings.LogDirectory;
        tbRetentionDays.Text    = _settings.LogRetentionDays.ToString();
        cbUseToast.IsChecked    = _settings.UseToast;
        UpdateLogOptionsEnabled();
    }

    private void btnOK_Click(object sender, RoutedEventArgs e)
    {
        if (!IsValidDateFormat(tbFormat.Text))
        {
            MessageBox.Show(
                string.Format(Res.Err_InvalidDateFormat, tbFormat.Text),
                Res.Err_InvalidDateFormat_Title,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            tabControl.SelectedIndex = 0;
            tbFormat.Focus();
            tbFormat.SelectAll();
            return;
        }

        if (cbEnableLog.IsChecked == true && string.IsNullOrWhiteSpace(tbLogDirectory.Text))
        {
            MessageBox.Show(Res.Err_LogDirEmpty, Res.Err_InputError_Title,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            tabControl.SelectedIndex = 2;
            tbLogDirectory.Focus();
            return;
        }

        if (cbEnableLog.IsChecked == true &&
            (!int.TryParse(tbRetentionDays.Text, out int days) || days < 0))
        {
            MessageBox.Show(Res.Err_RetentionDaysInvalid, Res.Err_InputError_Title,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            tabControl.SelectedIndex = 2;
            tbRetentionDays.Focus();
            tbRetentionDays.SelectAll();
            return;
        }

        // Backup tab
        _settings.Fmt = tbFormat.Text;

        if (rbPosBefore.IsChecked == true)       _settings.Pos = AppendPos.PosBefore;
        else if (rbPosAfter.IsChecked == true)   _settings.Pos = AppendPos.PosAfter;

        if (rbOpeRename.IsChecked == true)        _settings.Ope = OpeWhenExists.OpeRename;
        else if (rbOpeOverride.IsChecked == true) _settings.Ope = OpeWhenExists.OpeOverride;
        else if (rbOpeDelete.IsChecked == true)   _settings.Ope = OpeWhenExists.OpeDelete;

        _settings.ExcludeSymLinks = cbExcludeSymLinks.IsChecked == true;

        // Exclude filters tab
        _settings.ExcludeFilters = lbFilters.Items.Cast<string>().ToList();

        // Log tab
        _settings.EnableLog        = cbEnableLog.IsChecked == true;
        _settings.LogDirectory     = tbLogDirectory.Text.Trim();
        _settings.LogRetentionDays = int.TryParse(tbRetentionDays.Text, out int d) ? d : 30;
        _settings.UseToast         = cbUseToast.IsChecked == true;

        // Destination tab
        _settings.UseCustomDestDir = rbDestCustomDir.IsChecked == true;
        _settings.DestDir          = tbDestDir.Text.Trim();
        _settings.Save();
        Close();
    }

    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void btnAbout_Click(object sender, RoutedEventArgs e)
    {
        new AboutWindow { Owner = this }.ShowDialog();
    }

    // Enables or disables log input fields when the log checkbox changes
    private void cbEnableLog_Changed(object sender, RoutedEventArgs e)
    {
        UpdateLogOptionsEnabled();
    }

    private void UpdateLogOptionsEnabled()
    {
        bool enabled = cbEnableLog.IsChecked == true;
        pnlLogOptions.IsEnabled = enabled;
    }

    // --- Destination tab ---

    private void rbDest_Changed(object sender, RoutedEventArgs e)
    {
        if (pnlDestDir == null) return;
        pnlDestDir.IsEnabled = rbDestCustomDir.IsChecked == true;
    }

    private void btnBrowseDest_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = Res.Dialog_SelectDestFolder,
            InitialDirectory = Directory.Exists(tbDestDir.Text)
                ? tbDestDir.Text
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        };

        if (dialog.ShowDialog() == true)
            tbDestDir.Text = dialog.FolderName;
    }

    private void btnOpenDestDir_Click(object sender, RoutedEventArgs e)
    {
        string dir = tbDestDir.Text.Trim();
        if (string.IsNullOrWhiteSpace(dir))
        {
            MessageBox.Show(Res.Err_DestDirNotSet, "DropBackup",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!Directory.Exists(dir))
        {
            MessageBox.Show(string.Format(Res.Err_FolderNotFound, dir), "DropBackup",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        System.Diagnostics.Process.Start("explorer.exe", dir);
    }

    // --- Exclude filters tab ---

    private void btnFilterAdd_Click(object sender, RoutedEventArgs e)
    {
        AddFilterPattern();
    }

    private void tbFilterPattern_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            AddFilterPattern();
            e.Handled = true;
        }
    }

    private void AddFilterPattern()
    {
        string pattern = tbFilterPattern.Text.Trim();
        if (string.IsNullOrWhiteSpace(pattern)) return;
        if (!lbFilters.Items.Contains(pattern))
            lbFilters.Items.Add(pattern);
        tbFilterPattern.Clear();
        tbFilterPattern.Focus();
    }

    private void btnFilterDelete_Click(object sender, RoutedEventArgs e)
    {
        var selected = lbFilters.SelectedItems.Cast<string>().ToList();
        foreach (string item in selected)
            lbFilters.Items.Remove(item);
    }

    // Opens the log directory in Explorer
    private void btnOpenLogDir_Click(object sender, RoutedEventArgs e)
    {
        string dir = tbLogDirectory.Text.Trim();
        if (string.IsNullOrWhiteSpace(dir))
        {
            MessageBox.Show(Res.Err_LogDirNotSet, "DropBackup",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!Directory.Exists(dir))
        {
            MessageBox.Show(string.Format(Res.Err_FolderNotFound, dir), "DropBackup",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        System.Diagnostics.Process.Start("explorer.exe", dir);
    }

    // Folder browse dialog
    private void btnBrowse_Click(object sender, RoutedEventArgs e)
    {
        // OpenFolderDialog is available in .NET 10 WPF
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = Res.Dialog_SelectLogFolder,
            InitialDirectory = Directory.Exists(tbLogDirectory.Text)
                ? tbLogDirectory.Text
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        };

        if (dialog.ShowDialog() == true)
            tbLogDirectory.Text = dialog.FolderName;
    }

    private void tbFormat_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (tbPreview == null) return;
        string fmt = tbFormat.Text;
        if (string.IsNullOrWhiteSpace(fmt))
        {
            tbPreview.Text = "";
            return;
        }
        try
        {
            string preview = DateTime.Now.ToString(fmt);
            bool valid = preview.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
            tbPreview.Text = preview;
            tbPreview.Foreground = valid
                ? System.Windows.Media.Brushes.DarkGreen
                : System.Windows.Media.Brushes.Red;
        }
        catch (FormatException)
        {
            tbPreview.Text = Res.Msg_InvalidFormat;
            tbPreview.Foreground = System.Windows.Media.Brushes.Red;
        }
    }

    private static bool IsValidDateFormat(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
            return false;

        try
        {
            string result = DateTime.Now.ToString(format);
            return result.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    }
