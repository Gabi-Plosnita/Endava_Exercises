namespace ReadingList.App;

public record FileImportReport(
    string FileName,
    int Imported,
    int Duplicates,
    int Malformed);
