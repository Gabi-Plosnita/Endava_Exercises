namespace AirportTool.Application;

public class ImportResultDto
{
    public int Total { get; set; }

    public int Created { get; set; }

    public int Updated { get; set; }

    public int Failed { get; set; }

    public List<string> ErrorMessages { get; set; } = new();
}
