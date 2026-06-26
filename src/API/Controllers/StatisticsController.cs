using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HungStore.Application.Statistics.Interfaces;

namespace HungStore.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/statistics")]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;

    public StatisticsController(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _statisticsService.GetDashboardAsync();
        return Ok(result);
    }
}
