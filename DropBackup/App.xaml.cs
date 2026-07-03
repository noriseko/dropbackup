using System.Windows;
using Res = DropBackup.Properties.Resources;

namespace DropBackup;

public partial class App : Application
{
    private void App_Startup(object sender, StartupEventArgs e)
    {
        if (e.Args.Length == 0)
        {
            var window = new MainWindow();
            window.Show();
        }
        else
        {
            RunBackup(e.Args);
            Shutdown();
        }
    }

    private static void RunBackup(string[] args)
    {
        var settings = new Settings();
        settings.Load();
        var backup = new Backup(settings);

        int success = 0, failed = 0, skipped = 0;
        bool aborted = false;
        foreach (string arg in args)
        {
            var status = backup.Execute(arg);
            switch (status)
            {
                case BackupStatus.Success:  success++;  break;
                case BackupStatus.Skipped:  skipped++;  break;
                case BackupStatus.Failed:   failed++;   break;
                case BackupStatus.Aborted:
                    failed++;
                    aborted = true;
                    break;
            }
            if (aborted) break;
        }
        if (settings.EnableLog)
        {
            var logger = new BackupLogger(settings.LogDirectory, settings.LogRetentionDays);
            logger.WriteLog(success, failed, skipped, args, backup.SkippedSymLinks);
        }

        Notify(settings, success, failed, skipped, backup.SkippedSymLinks, backup.SkippedByFilter);
    }

    private static void Notify(Settings settings, int success, int failed, int skipped,
                                IReadOnlyList<string> skippedSymLinks,
                                IReadOnlyList<string> skippedByFilter)
    {
        if (settings.UseToast)
        {
            try
            {
                ToastHelper.ShowResult(success, failed, skipped);
                return;
            }
            catch
            {
                // Toast failed; fall back to the notification dialog.
            }
        }

        var win = new NotifyWindow(success, failed, skipped, skippedSymLinks, skippedByFilter);
        win.ShowDialog();
    }
}

