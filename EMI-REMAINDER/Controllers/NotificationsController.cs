using EMI_REMAINDER.Data;
using EMI_REMAINDER.DTOs;
using EMI_REMAINDER.DTOs.Notifications;
using EMI_REMAINDER.DTOs.Reminders;
using EMI_REMAINDER.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EMI_REMAINDER.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserService _userService;
    private readonly JwtService _jwtService;

    public NotificationsController(AppDbContext db, UserService userService, JwtService jwtService)
    {
        _db = db;
        _userService = userService;
        _jwtService = jwtService;
    }

    /// <summary>Register or update Firebase Cloud Messaging token</summary>
    [HttpPost("register-fcm")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> RegisterFcm([FromBody] RegisterFcmRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FcmToken))
            return BadRequest(ApiResponse.Fail("FCM token is required."));

        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var success = await _userService.UpdateFcmTokenAsync(userId.Value, request.FcmToken);
        if (!success) return NotFound(ApiResponse.Fail("User not found."));

        return Ok(ApiResponse.Ok("FCM token registered."));
    }

    /// <summary>Get notification history (sent reminders)</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _db.Reminders
            .Include(r => r.Bill)
            .Where(r => r.UserId == userId.Value && r.Status == "sent")
            .OrderByDescending(r => r.SentAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = items.Select(ReminderService.MapToResponse),
            pagination = new
            {
                page,
                pageSize,
                totalCount = total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        });
    }

    private int? GetUserId() => _jwtService.GetUserIdFromContext(HttpContext);
}
