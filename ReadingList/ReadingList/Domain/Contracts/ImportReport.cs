namespace ReadingList.Domain;

public record ImportReport(
    IReadOnlyList<FileImportReport> Files,
    int TotalImported,
    int TotalDuplicates,
    int TotalMalformed);
