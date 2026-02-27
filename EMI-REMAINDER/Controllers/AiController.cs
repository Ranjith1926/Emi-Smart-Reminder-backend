using EMI_REMAINDER.DTOs;
using EMI_REMAINDER.DTOs.AI;
using EMI_REMAINDER.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMI_REMAINDER.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
[Produces("application/json")]
public class AiController : ControllerBase
{
    private readonly AiService _aiService;
    private readonly JwtService _jwtService;

    public AiController(AiService aiService, JwtService jwtService)
    {
        _aiService = aiService;
        _jwtService = jwtService;
    }

    /// <summary>
    /// Generate an EMI/bill explanation with financial insights and tips.
    /// Returns markdown-formatted analysis including annual commitment and category-specific advice.
    /// </summary>
    [HttpPost("explain-emi")]
    [ProducesResponseType(typeof(ApiResponse<ExplainEmiResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> ExplainEmi([FromBody] ExplainEmiRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var (response, error) = await _aiService.ExplainEmiAsync(userId.Value, request.BillId);
        if (error is not null) return NotFound(ApiResponse.Fail(error));

        return Ok(ApiResponse<ExplainEmiResponse>.Ok(response!));
    }

    /// <summary>
    /// Generate a channel-appropriate reminder message for a bill.
    /// Supports: push, sms, whatsapp
    /// </summary>
    [HttpPost("reminder-message")]
    [ProducesResponseType(typeof(ApiResponse<ReminderMessageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GenerateReminderMessage([FromBody] ReminderMessageRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        if (!new[] { "push", "sms", "whatsapp" }.Contains(request.Channel.ToLower()))
            return BadRequest(ApiResponse.Fail("Channel must be one of: push, sms, whatsapp."));

        var (response, error) = await _aiService.GenerateReminderMessageAsync(
            userId.Value, request.BillId, request.Channel.ToLower());

        if (error is not null) return NotFound(ApiResponse.Fail(error));

        return Ok(ApiResponse<ReminderMessageResponse>.Ok(response!));
    }

    /// <summary>
    /// Generate monthly financial insights and summary for the user's bills.
    /// </summary>
    [HttpPost("monthly-insights")]
    [ProducesResponseType(typeof(ApiResponse<MonthlyInsightsResponse>), 200)]
    public async Task<IActionResult> MonthlyInsights([FromBody] MonthlyInsightsRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var response = await _aiService.GenerateMonthlyInsightsAsync(userId.Value, request.Month, request.Year);
        return Ok(ApiResponse<MonthlyInsightsResponse>.Ok(response));
    }

    private int? GetUserId() => _jwtService.GetUserIdFromContext(HttpContext);
}
