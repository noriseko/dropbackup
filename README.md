# DropBackup

A lightweight Windows desktop application for quick file and folder backups with timestamp-based naming.

![Version](https://img.shields.io/badge/version-1.0.0-blue)
![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)

## Overview

**DropBackup** is a simple yet powerful tool that creates timestamped backups of files and folders with just a drag-and-drop. Whether you need to preserve a working version before making changes or quickly archive important files, DropBackup makes it effortless.

## Features

- **Quick Backup Operation**
  - Drag and drop files/folders onto the app shortcut
  - Command-line support for batch operations
  - Automatic timestamp insertion in backup names

- **Flexible Timestamp Formatting**
  - Customizable date/time format (e.g., `yyyyMMdd-HHmmss`)
  - Choose position: before or after filename
  - Live preview of resulting backup name

- **Smart Handling of Existing Files**
  - Append sequential numbers (e.g., `file_20250101_001.txt`)
  - Overwrite existing files
  - Delete then copy (useful for read-only files)

- **Advanced Filtering**
  - Exclude symbolic links option
  - Wildcard-based exclude patterns (e.g., `*.tmp`, `Thumbs.db`)
  - Fine-grained control over what gets backed up

- **Backup Destination Control**
  - Same folder as source (default)
  - Custom destination folder
  - Preserves directory structure for folder backups

- **Logging & Notifications**
  - Optional backup log with configurable retention
  - Completion dialog or unobtrusive toast notification
  - Detailed operation summary

- **User-Friendly Interface**
  - Tabbed settings window (Backup / Filter / General)
  - Automatic UI language switching (Japanese/English based on system locale)
  - No-argument launch opens settings directly

## Requirements

- Windows 10 (1809) or later
- .NET 10.0 Runtime

## Installation

Download the latest release from the [Releases](https://github.com/noriseko/dropbackup/releases) page.

### Steps

1. **Install .NET 10.0 Desktop Runtime** (if not already installed)  
   Download from: [https://dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0)

2. **Download** `DropBackup-x.x.x-win-x64.zip` from the Releases page

3. **Extract** all files to a folder of your choice  
   Example: `C:\Program Files\DropBackup`

4. **(Optional)** Create a shortcut to `DropBackup.exe` on your desktop or Start Menu  
   This shortcut becomes your drop target for quick backups

5. **Run** `DropBackup.exe` without arguments to configure settings

### System Requirements

- Windows 10 (1809) or later
- .NET 10.0 Desktop Runtime

## Usage

### Launch Methods

1. **Settings Mode**: Run `DropBackup.exe` without arguments to open the settings window
2. **Drag and Drop**: Drag files or folders onto `DropBackup.exe` (or its shortcut) in Windows Explorer
3. **Command Line**: `DropBackup.exe "C:\path\to\file.txt" "C:\path\to\folder"`

### Basic Operations

#### Configuring Settings

1. Run `DropBackup.exe` without arguments
2. Configure your preferences in the tabbed interface:
   - **Backup tab**: Format, position, conflict handling, destination
   - **Filter tab**: Exclude patterns (e.g., `*.tmp`, `node_modules`)
   - **General tab**: Logging and notification preferences
3. Click "OK" to save

#### Creating a Backup

1. Drag a file or folder onto the `DropBackup.exe` shortcut
2. The backup is created instantly according to your settings
3. A notification shows the result

#### Example Backup Names

With format `yyyyMMdd-HHmmss` and position "Before":
- `20250601-143022_document.pdf` (file)
- `20250601-143022_ProjectFolder` (folder)

With format `yyyyMMdd-HHmmss` and position "After":
- `document_20250601-143022.pdf` (file)
- `ProjectFolder_20250601-143022` (folder)

### Advanced Features

#### Exclude Filters

Add patterns to skip specific files during folder backups:
- `*.tmp` - Skip all temporary files
- `Thumbs.db` - Skip Windows thumbnail cache
- `node_modules` - Skip npm packages folder

#### Custom Destination Folder

Choose "Custom folder" in Backup tab to:
- Store all backups in a dedicated location
- Separate backups from source files
- Centralize backup management

#### Symbolic Links

Enable "Exclude symbolic links" to skip:
- Symbolic file links
- Directory junctions
- Avoid infinite loop scenarios

## Technology Stack

- .NET 10.0
- WPF (Windows Presentation Foundation)
- C# with nullable reference types

## Project Structure

```
DropBackup/
├─ DropBackup/              # Main application project
│  ├─ Assets/               # Application icons and images
│  ├─ Docs/                 # Documentation (specs, TODO)
│  ├─ MainWindow.xaml       # Main settings window UI
│  ├─ Backup.cs             # Core backup logic
│  ├─ Settings.cs           # Configuration management
│  └─ ...
├─ DropBackup.Test/         # Unit tests
└─ DropBackup.slnx          # Solution file
```

## Building from Source

### Prerequisites
- Visual Studio 2026 or later
- .NET 10.0 SDK

### Steps
1. Clone the repository
   ```bash
   git clone https://github.com/noriseko/dropbackup.git
   cd dropbackup
   ```

2. Open `DropBackup.slnx` in Visual Studio

3. Build the solution (Ctrl+Shift+B)

4. Run the application (F5)

### Creating a Release Package

To create a release ZIP package for distribution:

```powershell
.\build-release.ps1 -Version "1.0.0"
```

This will generate `Release\DropBackup-x.x.x-win-x64.zip` ready for GitHub Releases.

## Configuration

Settings are stored in `%APPDATA%\DropBackup\Settings.ini` and can be edited manually if needed.

### Example Settings.ini

```ini
[Backup]
Format=yyyyMMdd-HHmmss
Position=Before
WhenExists=Rename
ExcludeSymLinks=True
UseCustomDestDir=False
DestDir=

[Filter]
Exclude=*.tmp|Thumbs.db|desktop.ini

[General]
SaveLog=True
LogDir=%APPDATA%\DropBackup\Logs
LogRetentionDays=30
ShowToast=True
```

## Uninstallation

Simply delete the folder containing DropBackup files. The settings file in `%APPDATA%\DropBackup` can also be removed if desired.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

Noriyuki Seko

---

*If you find this tool useful, feel free to star ⭐ the repository!*
