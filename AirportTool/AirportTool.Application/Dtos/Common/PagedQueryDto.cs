namespace AirportTool.Application;

public class PagedQueryDto
{
    public int PageIndex { get; init; } = 0;
    public int PageSize { get; init; } = 10;
}
