using System.Diagnostics;
using Res = DropBackup.Properties.Resources;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace DropBackup;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        var asm = Assembly.GetExecutingAssembly();

        // Application name
        string name = asm.GetCustomAttribute<AssemblyProductAttribute>()?.Product
                   ?? asm.GetName().Name
                   ?? "DropBackup";
        tbAppName.Text = name;

        // Version
        string version = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? asm.GetName().Version?.ToString()
                      ?? "";
        tbVersion.Text = string.Format(Res.About_Version, FormatVersion(version));

        // Copyright
        string copyright = asm.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "";
        tbCopyright.Text = copyright;
    }

    /// <summary>Strips +commit metadata from a version string and returns the result.</summary>
    internal static string FormatVersion(string version)
    {
        int plus = version.IndexOf('+');
        return plus >= 0 ? version[..plus] : version;
    }

    private void lnkGitHub_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
