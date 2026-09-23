namespace YtPump.Core.Settings;

public static class UrlHistory
{
    public const int MaxCount = 10;

    public static List<string> Remember(IEnumerable<string>? current, string url)
    {
        var normalized = (url ?? "").Trim();
        var list = new List<string>();
        if (current != null)
        {
            foreach (var item in current)
            {
                var trimmed = (item ?? "").Trim();
                if (trimmed.Length == 0
                    || string.Equals(trimmed, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                list.Add(trimmed);
            }
        }

        if (normalized.Length > 0)
        {
            list.Insert(0, normalized);
        }

        if (list.Count > MaxCount)
        {
            list.RemoveRange(MaxCount, list.Count - MaxCount);
        }

        return list;
    }

    public static List<string> Normalize(IEnumerable<string>? current)
    {
        var list = new List<string>();
        if (current == null)
        {
            return list;
        }

        foreach (var item in current)
        {
            var trimmed = (item ?? "").Trim();
            if (trimmed.Length == 0
                || list.Exists(existing => string.Equals(existing, trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            list.Add(trimmed);
            if (list.Count == MaxCount)
            {
                break;
            }
        }

        return list;
    }
}
