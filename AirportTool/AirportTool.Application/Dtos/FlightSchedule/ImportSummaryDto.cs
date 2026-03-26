namespace AirportTool.Application;

public class ImportSummaryDto
{
    public int Total { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Failed { get; set; }
    public List<ImportRowErrorDto> Errors { get; set; } = new();
}
