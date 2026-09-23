using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace YtPump.App;

internal static class HelpMarkdown
{
    public static string Load(string language)
    {
        var suffix = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "ru";
        var name = "YtPump.App.Help." + suffix + ".md";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
                           ?? throw new InvalidOperationException(name);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static FlowDocument ToDocument(string markdown)
    {
        var ink = Application.Current?.TryFindResource("Brush.Ink") as Brush ?? Brushes.Black;
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 13,
            Foreground = ink,
            Background = Brushes.Transparent,
            PagePadding = new Thickness(0)
        };

        var first = true;
        foreach (var raw in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var trimmed = raw.Trim();
            var paragraph = trimmed switch
            {
                _ when trimmed.StartsWith("# ", StringComparison.Ordinal) =>
                    Heading(trimmed[2..], 18, first ? 0 : 16),
                _ when trimmed.StartsWith("## ", StringComparison.Ordinal) =>
                    Heading(trimmed[3..], 15, first ? 0 : 14),
                _ when trimmed.StartsWith("- ", StringComparison.Ordinal) =>
                    Body("• " + trimmed[2..], 18, 4),
                _ => Body(trimmed, 0, 8)
            };
            paragraph.Foreground = ink;
            doc.Blocks.Add(paragraph);
            first = false;
        }

        return doc;
    }

    private static Paragraph Heading(string text, double size, double top)
    {
        var paragraph = new Paragraph
        {
            FontSize = size,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, top, 0, 6)
        };
        AddInlines(paragraph, text);
        return paragraph;
    }

    private static Paragraph Body(string text, double left, double bottom)
    {
        var paragraph = new Paragraph { Margin = new Thickness(left, 0, 0, bottom) };
        AddInlines(paragraph, text);
        return paragraph;
    }

    private static void AddInlines(Paragraph paragraph, string text)
    {
        var index = 0;
        while (index < text.Length)
        {
            var start = text.IndexOf("**", index, StringComparison.Ordinal);
            if (start < 0)
            {
                paragraph.Inlines.Add(new Run(text[index..]));
                return;
            }

            if (start > index)
            {
                paragraph.Inlines.Add(new Run(text[index..start]));
            }

            var end = text.IndexOf("**", start + 2, StringComparison.Ordinal);
            if (end < 0)
            {
                paragraph.Inlines.Add(new Run(text[start..]));
                return;
            }

            paragraph.Inlines.Add(new Run(text[(start + 2)..end]) { FontWeight = FontWeights.SemiBold });
            index = end + 2;
        }
    }
}
