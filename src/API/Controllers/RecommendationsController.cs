using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using HungStore.Application.Recommendations.Interfaces;

namespace HungStore.API.Controllers;

[ApiController]
[Route("api/recommendations")]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;

    public RecommendationsController(IRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecommendations()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var recommendations = await _recommendationService.GetPersonalizedRecommendationsAsync(userId);
        return Ok(recommendations);
    }
}
