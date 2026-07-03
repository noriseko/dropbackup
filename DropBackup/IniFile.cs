using System.IO;
using System.Text;

namespace DropBackup;

class IniFile
{
    // Reads a string value from an INI file.
    // Returns the number of characters copied (excluding the null terminator), matching the Win32 API contract.
    public static uint GetPrivateProfileString(
        string lpAppName, string lpKeyName, string lpDefault,
        StringBuilder lpReturnedString, uint nSize, string lpFileName)
    {
        lpReturnedString.Clear();
        string value = ReadValue(lpFileName, lpAppName, lpKeyName) ?? lpDefault;

        // Truncate to buffer size - 1 (reserve space for null terminator)
        if (value.Length >= nSize)
            value = value.Substring(0, (int)nSize - 1);

        lpReturnedString.Append(value);
        return (uint)value.Length;
    }

    // Reads an integer value from an INI file.
    // Returns nDefault if the key is not found or the value cannot be parsed.
    public static uint GetPrivateProfileInt(
        string lpAppName, string lpKeyName, int nDefault, string lpFileName)
    {
        string? raw = ReadValue(lpFileName, lpAppName, lpKeyName);
        if (raw != null && int.TryParse(raw, out int result))
            return (uint)result;
        return (uint)nDefault;
    }

    // Writes a string value to an INI file.
    // Returns true on success.
    public static bool WritePrivateProfileString(
        string lpAppName, string lpKeyName, string lpString, string lpFileName)
    {
        try
        {
            var lines = File.Exists(lpFileName)
                ? new List<string>(File.ReadAllLines(lpFileName, Encoding.UTF8))
                : new List<string>();

            int sectionIndex = FindSection(lines, lpAppName);
            if (sectionIndex < 0)
            {
                // Section not found: append section and key at end
                if (lines.Count > 0 && lines[^1].Trim().Length > 0)
                    lines.Add("");
                lines.Add($"[{lpAppName}]");
                lines.Add($"{lpKeyName}={lpString}");
            }
            else
            {
                // Section found: find key within section
                int keyIndex = FindKey(lines, sectionIndex, lpKeyName);
                if (keyIndex >= 0)
                    lines[keyIndex] = $"{lpKeyName}={lpString}";
                else
                {
                    // Key not found: insert before next section or at end
                    int insertAt = FindNextSectionOrEnd(lines, sectionIndex);
                    lines.Insert(insertAt, $"{lpKeyName}={lpString}");
                }
            }

            string? dir = Path.GetDirectoryName(lpFileName);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllLines(lpFileName, lines, Encoding.UTF8);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // --- Private helpers ---

    private static string? ReadValue(string filePath, string section, string key)
    {
        if (!File.Exists(filePath))
            return null;

        var lines = File.ReadAllLines(filePath, Encoding.UTF8);
        int sectionIndex = FindSection(lines, section);
        if (sectionIndex < 0)
            return null;

        int keyIndex = FindKey(lines, sectionIndex, key);
        if (keyIndex < 0)
            return null;

        int eq = lines[keyIndex].IndexOf('=');
        return eq >= 0 ? lines[keyIndex][(eq + 1)..].Trim() : null;
    }

    private static int FindSection(IList<string> lines, string section)
    {
        string header = $"[{section}]";
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Trim().Equals(header, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private static int FindKey(IList<string> lines, int sectionIndex, string key)
    {
        for (int i = sectionIndex + 1; i < lines.Count; i++)
        {
            string trimmed = lines[i].Trim();
            if (trimmed.StartsWith('['))
                break; // next section reached
            if (trimmed.StartsWith(';') || trimmed.StartsWith('#'))
                continue; // comment line
            int eq = trimmed.IndexOf('=');
            if (eq > 0 && trimmed[..eq].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private static int FindNextSectionOrEnd(IList<string> lines, int sectionIndex)
    {
        for (int i = sectionIndex + 1; i < lines.Count; i++)
        {
            if (lines[i].Trim().StartsWith('['))
                return i;
        }
        return lines.Count;
    }
}
