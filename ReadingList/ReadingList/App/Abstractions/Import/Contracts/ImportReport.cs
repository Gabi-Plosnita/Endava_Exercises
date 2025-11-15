namespace ReadingList.App;

public record ImportReport(
    IReadOnlyList<FileImportReport> Files,
    int TotalImported,
    int TotalDuplicates,
    int TotalMalformed)
{
    public override string ToString()
    {
        return $"Imported: {TotalImported}\nDuplicates: {TotalDuplicates}\nMalformed: {TotalMalformed}.";
    }
}
