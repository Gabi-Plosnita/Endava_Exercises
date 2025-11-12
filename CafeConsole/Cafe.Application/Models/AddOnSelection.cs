namespace Cafe.Application;

public record AddOnSelection(
    AddOnType AddOnType,
    IReadOnlyDictionary<string, string>? Parameters = null
);

