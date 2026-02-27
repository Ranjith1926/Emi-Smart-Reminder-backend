using EMI_REMAINDER.DTOs;
using EMI_REMAINDER.DTOs.Reminders;
using EMI_REMAINDER.Models;
using EMI_REMAINDER.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMI_REMAINDER.Controllers;

[ApiController]
[Route("api/reminders")]
[Authorize]
[Produces("application/json")]
public class RemindersController : ControllerBase
{
    private readonly ReminderService _reminderService;
    private readonly BillService _billService;
    private readonly JwtService _jwtService;

    public RemindersController(ReminderService reminderService, BillService billService, JwtService jwtService)
    {
        _reminderService = reminderService;
        _billService = billService;
        _jwtService = jwtService;
    }

    /// <summary>Get reminders for current user with optional filters</summary>
    [HttpGet]
    public async Task<IActionResult> GetReminders([FromQuery] ReminderQueryParams query)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var (items, total) = await _reminderService.GetRemindersAsync(
            userId.Value, query.Status, query.BillId, query.Page, query.PageSize);

        return Ok(new
        {
            success = true,
            data = items,
            pagination = new
            {
                page = query.Page,
                pageSize = query.PageSize,
                totalCount = total,
                totalPages = (int)Math.Ceiling(total / (double)query.PageSize)
            }
        });
    }

    /// <summary>Reschedule a reminder to a new date/time</summary>
    [HttpPut("{id:int}/reschedule")]
    [ProducesResponseType(typeof(ApiResponse<ReminderResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RescheduleReminder(int id, [FromBody] RescheduleReminderRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        if (request.ReminderDate <= DateTime.UtcNow)
            return BadRequest(ApiResponse.Fail("Reminder date must be in the future."));

        var reminder = await _reminderService.RescheduleReminderAsync(id, userId.Value, request.ReminderDate);
        if (reminder is null) return NotFound(ApiResponse.Fail("Reminder not found."));

        return Ok(ApiResponse<ReminderResponse>.Ok(reminder));
    }

    /// <summary>Delete a reminder</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteReminder(int id)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var deleted = await _reminderService.DeleteReminderAsync(id, userId.Value);
        if (!deleted) return NotFound(ApiResponse.Fail("Reminder not found."));

        return Ok(ApiResponse.Ok("Reminder deleted."));
    }

    /// <summary>Send a test reminder for a specific bill</summary>
    [HttpPost("send-test")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SendTestReminder([FromBody] SendTestReminderRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var bill = await _billService.GetBillByIdAsync(request.BillId, userId.Value);
        if (bill is null) return NotFound(ApiResponse.Fail("Bill not found."));

        var message = ReminderService.BuildReminderMessage(
            new Bill
            {
                Id = bill.Id,
                Title = bill.Title,
                Amount = bill.Amount,
                DueDate = bill.DueDate,
                Category = bill.Category
            },
            daysBefore: (bill.DueDate.Date - DateTime.UtcNow.Date).Days,
            channel: request.Channel);

        // In a real app, fire the actual notification here
        return Ok(new { success = true, message = "Test reminder sent.", preview = message });
    }

    private int? GetUserId() => _jwtService.GetUserIdFromContext(HttpContext);
}
