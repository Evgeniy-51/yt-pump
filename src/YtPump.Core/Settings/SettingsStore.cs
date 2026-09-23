using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using YtPump.Core.Tools;
using YtPump.Core.Validation;

namespace YtPump.Core.Settings;

public sealed class SettingsStore
{
    public const string PortableFlagName = "portable.flag";
    public const string DataFolderName = "data";
    public const string FileName = "settings.json";
    public const string BackupFileName = "settings.json.bak";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly string _appDirectory;
    private readonly string _userConfigDirectory;

    public SettingsStore(string appDirectory, string userConfigDirectory)
    {
        _appDirectory = Path.GetFullPath(appDirectory);
        _userConfigDirectory = Path.GetFullPath(userConfigDirectory);
    }

    public static SettingsStore ForCurrentProcess() =>
        new(
            ToolsLocator.GetAppDirectory(),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "YtPump"));

    public bool IsPortable => File.Exists(Path.Combine(_appDirectory, PortableFlagName));

    public string SettingsDirectory =>
        IsPortable ? Path.Combine(_appDirectory, DataFolderName) : _userConfigDirectory;

    public string SettingsPath => Path.Combine(SettingsDirectory, FileName);

    public SettingsLoadResult Load(string defaultLanguage, string defaultOutputDirectory)
    {
        var portable = IsPortable;
        var path = SettingsPath;

        if (portable && !CanUsePortableDirectory())
        {
            var fallback = CreateDefault(defaultLanguage, defaultOutputDirectory);
            return new SettingsLoadResult(
                fallback,
                path,
                IsPortable: true,
                UsedDefaults: true,
                IsReadOnly: true,
                SettingsWarning.PortableNotWritable);
        }

        if (!File.Exists(path))
        {
            return new SettingsLoadResult(
                CreateDefault(defaultLanguage, defaultOutputDirectory),
                path,
                portable,
                UsedDefaults: true,
                IsReadOnly: false,
                SettingsWarning.None);
        }

        try
        {
            var json = File.ReadAllText(path, Utf8NoBom);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
                           ?? CreateDefault(defaultLanguage, defaultOutputDirectory);
            settings.Normalize();
            if (string.IsNullOrWhiteSpace(settings.LastOutputDirectory))
            {
                settings.LastOutputDirectory = defaultOutputDirectory;
            }

            return new SettingsLoadResult(
                settings,
                path,
                portable,
                UsedDefaults: false,
                IsReadOnly: false,
                SettingsWarning.None);
        }
        catch (JsonException)
        {
            TryBackupCorruptFile(path);
            return new SettingsLoadResult(
                CreateDefault(defaultLanguage, defaultOutputDirectory),
                path,
                portable,
                UsedDefaults: true,
                IsReadOnly: false,
                SettingsWarning.CorruptJson);
        }
        catch (IOException)
        {
            TryBackupCorruptFile(path);
            return new SettingsLoadResult(
                CreateDefault(defaultLanguage, defaultOutputDirectory),
                path,
                portable,
                UsedDefaults: true,
                IsReadOnly: false,
                SettingsWarning.CorruptJson);
        }
    }

    public bool TrySave(AppSettings settings, out SettingsWarning error)
    {
        error = SettingsWarning.None;
        settings.Normalize();

        if (IsPortable && !CanUsePortableDirectory())
        {
            error = SettingsWarning.PortableNotWritable;
            return false;
        }

        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            var path = SettingsPath;
            var tmp = path + ".tmp";
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(tmp, json, Utf8NoBom);
            if (File.Exists(path))
            {
                File.Replace(tmp, path, destinationBackupFileName: null);
            }
            else
            {
                File.Move(tmp, path);
            }

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            error = IsPortable ? SettingsWarning.PortableNotWritable : SettingsWarning.IoError;
            return false;
        }
    }

    private bool CanUsePortableDirectory()
    {
        var dir = SettingsDirectory;
        try
        {
            if (File.Exists(dir))
            {
                return false;
            }

            Directory.CreateDirectory(dir);
            return DirectoryValidator.CanWrite(dir);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void TryBackupCorruptFile(string path)
    {
        try
        {
            var bak = Path.Combine(Path.GetDirectoryName(path)!, BackupFileName);
            File.Copy(path, bak, overwrite: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static AppSettings CreateDefault(string language, string outputDirectory)
    {
        var settings = new AppSettings
        {
            Language = UiLanguage.Normalize(language),
            LastOutputDirectory = outputDirectory
        };
        settings.Normalize();
        return settings;
    }
}
