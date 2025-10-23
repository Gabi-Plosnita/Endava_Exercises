namespace ReadingList.Domain;

public record FileImportReport(
    string FileName,
    int Imported,
    int Duplicates,
    int Malformed);
