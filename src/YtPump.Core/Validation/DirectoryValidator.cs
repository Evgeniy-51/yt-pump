namespace YtPump.Core.Validation;

public enum DirectoryStatus
{
    Ok,
    Empty,
    Missing,
    NotADirectory,
    NotWritable
}

public static class DirectoryValidator
{
    public static DirectoryStatus Check(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return DirectoryStatus.Empty;
        }

        if (File.Exists(path))
        {
            return DirectoryStatus.NotADirectory;
        }

        if (!Directory.Exists(path))
        {
            return DirectoryStatus.Missing;
        }

        return CanWrite(path) ? DirectoryStatus.Ok : DirectoryStatus.NotWritable;
    }

    public static bool CanWrite(string directory)
    {
        var probe = Path.Combine(directory, $".ytpump-write-{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllBytes(probe, [0]);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
        finally
        {
            try
            {
                if (File.Exists(probe))
                {
                    File.Delete(probe);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
