namespace YtPump.App;

public sealed class OptionItem
{
    public OptionItem(string id, string label)
    {
        Id = id;
        Label = label;
    }

    public string Id { get; }
    public string Label { get; set; }
    public override string ToString() => Label;
}
