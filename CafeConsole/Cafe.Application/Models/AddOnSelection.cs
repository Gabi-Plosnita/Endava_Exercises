namespace Cafe.Application;

public sealed record AddOnSelection(
    AddOnType AddOnType,
    IReadOnlyDictionary<string, string>? Parameters = null
);

