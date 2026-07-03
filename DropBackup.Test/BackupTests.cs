using System.IO;
using DropBackup;
using Xunit;

namespace DropBackup.Test;

public class BackupTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FakeErrorReporter _reporter;

    public BackupTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "DuplicateTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _reporter = new FakeErrorReporter();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            foreach (string f in Directory.GetFiles(_tempDir, "*", SearchOption.AllDirectories))
                File.SetAttributes(f, FileAttributes.Normal);
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private Backup CreateBackup(
        OpeWhenExists ope = OpeWhenExists.OpeRename,
        AppendPos pos = AppendPos.PosAfter,
        string fmt = "'FIXED'",
        bool excludeSymLinks = false)
        => new Backup(ope, pos, fmt, excludeSymLinks, _reporter);

    private string CreateTempFile(string name, string content = "test")
    {
        string path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, content);
        return path;
    }

    private string CreateTempDir(string name)
    {
        string path = Path.Combine(_tempDir, name);
        Directory.CreateDirectory(path);
        return path;
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void Execute_File_PosAfter_CreatesBackupWithDateSuffix()
    {
        string src = CreateTempFile("notes.txt", "hello");
        var backup = CreateBackup(pos: AppendPos.PosAfter, fmt: "'FIXED'");

        var result = backup.Execute(src);

        Assert.Equal(BackupStatus.Success, result);
        string expected = Path.Combine(_tempDir, "notes FIXED.txt");
        Assert.True(File.Exists(expected), $"Expected file not found: {expected}");
        Assert.Equal("hello", File.ReadAllText(expected));
    }

    [Fact]
    public void Execute_File_PosBefore_CreatesBackupWithDatePrefix()
    {
        string src = CreateTempFile("notes.txt", "world");
        var backup = CreateBackup(pos: AppendPos.PosBefore, fmt: "'FIXED'");

        var result = backup.Execute(src);

        Assert.Equal(BackupStatus.Success, result);
        string expected = Path.Combine(_tempDir, "FIXED notes.txt");
        Assert.True(File.Exists(expected), $"Expected file not found: {expected}");
    }

    [Fact]
    public void Execute_File_NoExtension_CreatesBackup()
    {
        string src = CreateTempFile("README");
        var backup = CreateBackup(pos: AppendPos.PosAfter, fmt: "'FIXED'");

        var result = backup.Execute(src);

        Assert.Equal(BackupStatus.Success, result);
        Assert.True(File.Exists(Path.Combine(_tempDir, "README FIXED")));
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void Execute_File_OpeOverride_OverwritesExistingBackup()
    {
        string src = CreateTempFile("doc.txt", "new content");
        string existing = Path.Combine(_tempDir, "doc FIXED.txt");
        File.WriteAllText(existing, "old content");

        var backup = CreateBackup(ope: OpeWhenExists.OpeOverride, pos: AppendPos.PosAfter, fmt: "'FIXED'");
        var result = backup.Execute(src);

        Assert.Equal(BackupStatus.Success, result);
        Assert.Equal("new content", File.ReadAllText(existing));
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void Execute_File_OpeDelete_DeletesExistingAndCreatesBackup()
    {
        string src = CreateTempFile("doc.txt", "new");
        string existing = Path.Combine(_tempDir, "doc FIXED.txt");
        File.WriteAllText(existing, "old");

        var backup = CreateBackup(ope: OpeWhenExists.OpeDelete, pos: AppendPos.PosAfter, fmt: "'FIXED'");
        var result = backup.Execute(src);

        Assert.Equal(BackupStatus.Success, result);
        Assert.Equal("new", File.ReadAllText(existing));
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void Execute_File_OpeRename_AddsSequentialSuffixWhenBackupExists()
    {
        string src = CreateTempFile("img.png", "data");
        File.WriteAllText(Path.Combine(_tempDir, "img FIXED.png"), "old");

        var backup = CreateBackup(ope: OpeWhenExists.OpeRename, pos: AppendPos.PosAfter, fmt: "'FIXED'");
        var result = backup.Execute(src);

        Assert.Equal(BackupStatus.Success, result);
        Assert.True(File.Exists(Path.Combine(_tempDir, "img FIXED-1.png")));
    }

    [Fact]
    public void Execute_File_OpeRename_AddsSecondSuffixWhenFirstAlsoExists()
    {
        string src = CreateTempFile("img.png", "data");
        File.WriteAllText(Path.Combine(_tempDir, "img FIXED.png"), "v0");
        File.WriteAllText(Path.Combine(_tempDir, "img FIXED-1.png"), "v1");

        var backup = CreateBackup(ope: OpeWhenExists.OpeRename, pos: AppendPos.PosAfter, fmt: "'FIXED'");
        var result = backup.Execute(src);

        Assert.Equal(BackupStatus.Success, result);
        Assert.True(File.Exists(Path.Combine(_tempDir, "img FIXED-2.png")));
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void Execute_Directory_CopiesAllFilesAndSubDirectories()
    {
        string srcDir = CreateTempDir("project");
        File.WriteAllText(Path.Combine(srcDir, "a.txt"), "a");
        string sub = Path.Combine(srcDir, "sub");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "b.txt"), "b");

        var backup = CreateBackup(pos: AppendPos.PosAfter, fmt: "'FIXED'");
        var result = backup.Execute(srcDir);

        Assert.Equal(BackupStatus.Success, result);
        string dstDir = Path.Combine(_tempDir, "project FIXED");
        Assert.True(Directory.Exists(dstDir));
        Assert.True(File.Exists(Path.Combine(dstDir, "a.txt")));
        Assert.True(File.Exists(Path.Combine(dstDir, "sub", "b.txt")));
        Assert.Equal("a", File.ReadAllText(Path.Combine(dstDir, "a.txt")));
        Assert.Equal("b", File.ReadAllText(Path.Combine(dstDir, "sub", "b.txt")));
    }

    [Fact]
    public void Execute_Directory_OpeRename_AddsSequentialSuffix()
    {
        string srcDir = CreateTempDir("project");
        File.WriteAllText(Path.Combine(srcDir, "a.txt"), "a");
        Directory.CreateDirectory(Path.Combine(_tempDir, "project FIXED"));

        var backup = CreateBackup(ope: OpeWhenExists.OpeRename, pos: AppendPos.PosAfter, fmt: "'FIXED'");
        var result = backup.Execute(srcDir);

        Assert.Equal(BackupStatus.Success, result);
        Assert.True(Directory.Exists(Path.Combine(_tempDir, "project FIXED-1")));
    }

    [Fact]
    public void Execute_Directory_OpeDelete_DeletesExistingDirAndCreatesBackup()
    {
        string srcDir = CreateTempDir("proj");
        File.WriteAllText(Path.Combine(srcDir, "new.txt"), "new");

        string existingBackup = Path.Combine(_tempDir, "proj FIXED");
        Directory.CreateDirectory(existingBackup);
        File.WriteAllText(Path.Combine(existingBackup, "old.txt"), "old");

        var backup = CreateBackup(ope: OpeWhenExists.OpeDelete, pos: AppendPos.PosAfter, fmt: "'FIXED'");
        var result = backup.Execute(srcDir);

        Assert.Equal(BackupStatus.Success, result);
        Assert.True(File.Exists(Path.Combine(existingBackup, "new.txt")));
        Assert.False(File.Exists(Path.Combine(existingBackup, "old.txt")));
    }

    [Fact]
    public void Execute_Directory_OpeOverride_MergesIntoExistingBackupDir()
    {
        string srcDir = CreateTempDir("proj2");
        File.WriteAllText(Path.Combine(srcDir, "new.txt"), "new content");

        string existingBackup = Path.Combine(_tempDir, "proj2 FIXED");
        Directory.CreateDirectory(existingBackup);
        File.WriteAllText(Path.Combine(existingBackup, "old.txt"), "old content");

        var backup = CreateBackup(ope: OpeWhenExists.OpeOverride, pos: AppendPos.PosAfter, fmt: "'FIXED'");
        var result = backup.Execute(srcDir);

        Assert.Equal(BackupStatus.Success, result);
        Assert.True(File.Exists(Path.Combine(existingBackup, "new.txt")));
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void Execute_NonExistentPath_ReturnsFalse()
    {
        var backup = CreateBackup();
        var result = backup.Execute(Path.Combine(_tempDir, "ghost.txt"));
        Assert.Equal(BackupStatus.Failed, result);
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void Execute_File_OpeRename_ReportsErrorWhenAllSuffixesUsed()
    {
        string src = CreateTempFile("x.txt", "data");
        File.WriteAllText(Path.Combine(_tempDir, "x FIXED.txt"), "v0");
        for (int i = 1; i < 99; i++)
            File.WriteAllText(Path.Combine(_tempDir, $"x FIXED-{i}.txt"), $"v{i}");

        var backup = CreateBackup(ope: OpeWhenExists.OpeRename, pos: AppendPos.PosAfter, fmt: "'FIXED'");
        var result = backup.Execute(src);

        Assert.NotEqual(BackupStatus.Success, result);
        Assert.NotEmpty(_reporter.Reports);
    }

    // -------------------------------------------------------------------
    // ���s���̑��s����
    // -------------------------------------------------------------------

    [Fact]
    public void Execute_ContinueOnError_ProcessesNextFileAfterFailure()
    {
        // 1����: �R�s�[���ǂݎ���p�f�B���N�g���ɂ��ċ������s������
        string src1 = CreateTempFile("fail.txt", "data1");
        string backupName1 = Path.Combine(_tempDir, "fail FIXED.txt");
        // �ǂݎ���p�t�@�C���Ƃ��Đ�ɔz�u�� OpeOverride �ŃR�s�[����s������
        File.WriteAllText(backupName1, "locked");
        File.SetAttributes(backupName1, FileAttributes.ReadOnly);

        // 2����: ����Ƀo�b�N�A�b�v�ł���t�@�C��
        string src2 = CreateTempFile("ok.txt", "data2");

        _reporter.ContinueOnError = true;
        var backup = new Backup(OpeWhenExists.OpeOverride, AppendPos.PosAfter, "'FIXED'", false, _reporter);

        backup.Execute(src1); // ���s �� ���s
        var result2 = backup.Execute(src2);

        // 1���ڂ̓G���[�񍐂���Ă���
        Assert.NotEmpty(_reporter.Reports);
        // 2���ڂ͐������Ă���
        Assert.Equal(BackupStatus.Success, result2);
        Assert.True(File.Exists(Path.Combine(_tempDir, "ok FIXED.txt")));
    }

    [Fact]
    public void Execute_AbortOnError_StopsProcessingAfterFailure()
    {
        string src1 = CreateTempFile("fail.txt", "data1");
        string backupName1 = Path.Combine(_tempDir, "fail FIXED.txt");
        File.WriteAllText(backupName1, "locked");
        File.SetAttributes(backupName1, FileAttributes.ReadOnly);

        string src2 = CreateTempFile("ok.txt", "data2");

        _reporter.ContinueOnError = false; // �f�t�H���g: ���~
        var backup = new Backup(OpeWhenExists.OpeOverride, AppendPos.PosAfter, "'FIXED'", false, _reporter);

        var result1 = backup.Execute(src1); // ���s �� ���~

        // �Ăяo������ Aborted ��󂯎������㑱����s���Ȃ��iApp.xaml.cs �� break �����j
        Assert.Equal(BackupStatus.Aborted, result1);
        // src2 �͎��s���Ȃ��̂� backup �t�@�C���͑��݂��Ȃ�
        Assert.False(File.Exists(Path.Combine(_tempDir, "ok FIXED.txt")));
    }

    // -------------------------------------------------------------------
    // �񑶍݃p�X + excludeSymLinks�iIsSymbolicLink �̗�O�o�O�C���m�F�j
    // -------------------------------------------------------------------

    [Fact]
    public void Execute_NonExistentPath_WithExcludeSymLinks_ReturnsFailed()
    {
        // excludeSymLinks=true �̂Ƃ� IsSymbolicLink ����O�𓊂��Ȃ����Ƃ�m�F
        var backup = CreateBackup(excludeSymLinks: true);
        var result = backup.Execute(Path.Combine(_tempDir, "no_such_file.txt"));
        Assert.Equal(BackupStatus.Failed, result);
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void Execute_File_OpeRename_ErrorMessageContainsSourcePath()
    {
        string src = CreateTempFile("x.txt", "data");
        File.WriteAllText(Path.Combine(_tempDir, "x FIXED.txt"), "v0");
        for (int i = 1; i < 99; i++)
            File.WriteAllText(Path.Combine(_tempDir, $"x FIXED-{i}.txt"), $"v{i}");

        var backup = CreateBackup(ope: OpeWhenExists.OpeRename, pos: AppendPos.PosAfter, fmt: "'FIXED'");
        backup.Execute(src);

        Assert.NotEmpty(_reporter.Reports);
        Assert.Contains(src, _reporter.Reports[^1].Message);
    }

    // -------------------------------------------------------------------
    // SkippedSymLinks �������
    // -------------------------------------------------------------------

    [Fact]
    public void SkippedSymLinks_IsEmptyInitially()
    {
        var backup = CreateBackup();
        Assert.Empty(backup.SkippedSymLinks);
    }

    [Fact]
    public void SkippedSymLinks_IsEmptyWhenExcludeSymLinksIsFalse()
    {
        // excludeSymLinks=false �Ȃ�V���{���b�N�����N�ł�X�L�b�v����Ȃ� �� ���X�g�͋�̂܂�
        string src = CreateTempFile("a.txt", "data");
        var backup = CreateBackup(excludeSymLinks: false);
        backup.Execute(src);
        Assert.Empty(backup.SkippedSymLinks);
    }

    // -------------------------------------------------------------------
    // -------------------------------------------------------------------

    [Fact]
    public void Backup_ViaSettingsConstructor_UsesSettingsValues()
    {
        string src = CreateTempFile("via_settings.txt", "content");
        string iniPath = Path.Combine(_tempDir, "test.ini");
        var settings = new Settings(iniPath)
        {
            Ope = OpeWhenExists.OpeRename,
            Pos = AppendPos.PosAfter,
            Fmt = "'FIXED'",
            ExcludeSymLinks = false,
            UseCustomDestDir = false,
        };
        var backup = new Backup(settings, _reporter);
        var result = backup.Execute(src);

        Assert.Equal(BackupStatus.Success, result);
        Assert.True(File.Exists(Path.Combine(_tempDir, "via_settings FIXED.txt")));
    }

    [Fact]
    public void Backup_ViaSettingsConstructor_UseCustomDestDir()
    {
        string destDir = Path.Combine(_tempDir, "custom_dest");
        Directory.CreateDirectory(destDir);
        string src = CreateTempFile("dest_test.txt", "hello");
        string iniPath = Path.Combine(_tempDir, "test2.ini");
        var settings = new Settings(iniPath)
        {
            Pos = AppendPos.PosAfter,
            Fmt = "'FIXED'",
            ExcludeSymLinks = false,
            UseCustomDestDir = true,
            DestDir = destDir,
        };
        var backup = new Backup(settings, _reporter);
        var result = backup.Execute(src);

        Assert.Equal(BackupStatus.Success, result);
        Assert.Single(Directory.GetFiles(destDir));
        Assert.False(File.Exists(Path.Combine(_tempDir, "dest_test FIXED.txt")));
    }
}
