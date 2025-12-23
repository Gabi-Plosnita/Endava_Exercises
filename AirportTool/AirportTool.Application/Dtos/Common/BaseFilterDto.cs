using System.ComponentModel.DataAnnotations;

namespace AirportTool.Application;

public class BaseFilterDto
{
    [Range(0, int.MaxValue, ErrorMessage = "PageIndex must be greater than or equal to 0.")]
    public int PageIndex { get; init; } = 0;

    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; init; } = 10;
}
