namespace YtPump.Core.Settings;

public sealed class WindowSettings
{
    public double Width { get; set; } = 920;
    public double Height { get; set; } = 640;
    public double? Left { get; set; }
    public double? Top { get; set; }
    public bool Maximized { get; set; }

    public void Normalize()
    {
        if (Width < 520)
        {
            Width = 520;
        }

        if (Height < 400)
        {
            Height = 400;
        }
    }
}
