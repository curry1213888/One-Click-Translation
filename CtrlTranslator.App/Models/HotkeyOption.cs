namespace CtrlTranslator.App.Models;

public sealed class HotkeyOption
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required string HotkeyValue { get; init; }
    public bool IsCustom { get; init; }

    public override string ToString() => Label;
}
