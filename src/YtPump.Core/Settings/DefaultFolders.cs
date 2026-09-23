namespace YtPump.Core.Settings;

public static class DefaultFolders
{
    public static string GetOutputDirectory()
    {
        var videos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        if (!string.IsNullOrWhiteSpace(videos) && Directory.Exists(videos))
        {
            return videos;
        }

        var downloads = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");
        if (Directory.Exists(downloads))
        {
            return downloads;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }
}
