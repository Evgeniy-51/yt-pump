namespace YtPump.Core.Settings;

public enum SettingsWarning
{
    None,
    CorruptJson,
    PortableNotWritable,
    IoError
}

public sealed record SettingsLoadResult(
    AppSettings Settings,
    string SettingsPath,
    bool IsPortable,
    bool UsedDefaults,
    bool IsReadOnly,
    SettingsWarning Warning);
