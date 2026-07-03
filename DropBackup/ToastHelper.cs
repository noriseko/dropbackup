using System.IO;
using Microsoft.Win32;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Res = DropBackup.Properties.Resources;

namespace DropBackup;

/// <summary>Helper for sending Windows toast notifications from an unpackaged WPF app.</summary>
internal static class ToastHelper
{
    private const string AppId = "DropBackup";

    /// <summary>Shows a rich toast summarising a completed backup run.</summary>
    public static void ShowResult(int success, int failed, int skipped)
    {
        EnsureAppIdRegistered();

        string line2 = $"{Res.Notify_LabelSuccess} {success}   " +
                       $"{Res.Notify_LabelFailed} {failed}   " +
                       $"{Res.Notify_LabelSkipped} {skipped}";

        string xml = $"""
            <toast duration="short">
              <visual>
                <binding template="ToastGeneric">
                  <text>{Escape(Res.NotifyWindow_Title)}</text>
                  <text>{Escape(line2)}</text>
                  <image placement="appLogoOverride" hint-crop="circle"
                         src="file:///{Escape(GetLogoPath())}"/>
                </binding>
              </visual>
              <audio src="ms-winsoundevent:Notification.Default"/>
            </toast>
            """;

        var doc = new XmlDocument();
        doc.LoadXml(xml);
        var notifier = ToastNotificationManager.CreateToastNotifier(AppId);
        notifier.Show(new ToastNotification(doc));
    }

    /// <summary>Shows a simple two-line toast (used as a fallback).</summary>
    public static void Show(string title, string body)
    {
        // Unpackaged apps must register the AppId in the registry before showing toasts.
        EnsureAppIdRegistered();

        string xml = $"""
            <toast>
              <visual>
                <binding template="ToastGeneric">
                  <text>{Escape(title)}</text>
                  <text>{Escape(body)}</text>
                </binding>
              </visual>
            </toast>
            """;

        var doc = new XmlDocument();
        doc.LoadXml(xml);
        var notifier = ToastNotificationManager.CreateToastNotifier(AppId);
        notifier.Show(new ToastNotification(doc));
    }

    private static string GetLogoPath()
    {
        string dir = AppContext.BaseDirectory;
        string png = Path.Combine(dir, "Assets", "Square44x44Logo.scale-200.png");
        return File.Exists(png) ? png.Replace('\\', '/') : "";
    }

    /// <summary>
    /// Registers <c>HKCU\Software\Classes\AppUserModelId\{AppId}</c> so that
    /// an unpackaged WPF app can send toast notifications.
    /// </summary>
    private static void EnsureAppIdRegistered()
    {
        const string keyPath = @"Software\Classes\AppUserModelId\" + AppId;
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath, writable: true);
        if (key.GetValue("DisplayName") is null)
            key.SetValue("DisplayName", "DropBackup");
    }

    private static string Escape(string s) =>
        s.Replace("&", "&amp;")
         .Replace("<", "&lt;")
         .Replace(">", "&gt;")
         .Replace("\"", "&quot;");
}
