namespace AirportTool.Application;

public class BaseFilterDto
{
    public int PageIndex { get; init; } = 0;

    public int PageSize { get; init; } = 10;
}
