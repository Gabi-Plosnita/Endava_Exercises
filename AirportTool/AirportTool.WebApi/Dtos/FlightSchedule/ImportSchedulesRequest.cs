using Microsoft.AspNetCore.Mvc;

namespace AirportTool.WebApi;

public class ImportSchedulesRequest
{
    [FromForm(Name = "file")]
    public IFormFile File { get; set; } = default!;
}
