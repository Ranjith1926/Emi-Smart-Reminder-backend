using EMI_REMAINDER.Data;
using EMI_REMAINDER.DTOs.Bills;
using EMI_REMAINDER.DTOs.Dashboard;
using EMI_REMAINDER.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EMI_REMAINDER.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwtService;

    public DashboardController(AppDbContext db, JwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    /// <summary>Overall financial summary for the dashboard</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        var next7Days = today.AddDays(7);

        var bills = await _db.Bills.Where(b => b.UserId == userId.Value).ToListAsync();

        var totalDue = bills.Where(b => b.Status != "paid")
                           .Sum(b => b.Amount);

        var totalOverdue = bills.Where(b => b.Status != "paid" && b.DueDate.Date < today)
                               .Sum(b => b.Amount);

        var totalPaidThisMonth = bills.Where(b => b.Status == "paid"
                                               && b.DueDate.Date >= monthStart
                                               && b.DueDate.Date <= monthEnd)
                                     .Sum(b => b.Amount);

        var billsDueNext7 = bills.Count(b => b.Status != "paid"
                                          && b.DueDate.Date >= today
                                          && b.DueDate.Date <= next7Days);

        var overdueCount = bills.Count(b => b.Status != "paid" && b.DueDate.Date < today);

        return Ok(new
        {
            success = true,
            data = new DashboardSummary
            {
                TotalDueAmount = totalDue,
                TotalOverdueAmount = totalOverdue,
                TotalPaidThisMonth = totalPaidThisMonth,
                BillsDueNext7Days = billsDueNext7,
                BillsOverdue = overdueCount,
                TotalBills = bills.Count,
                PaidBills = bills.Count(b => b.Status == "paid"),
                PendingBills = bills.Count(b => b.Status != "paid")
            }
        });
    }

    /// <summary>Bills due within N days (default 7)</summary>
    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming([FromQuery] int days = 7)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        if (days < 1 || days > 90) days = 7;

        var today = DateTime.UtcNow.Date;
        var cutoff = today.AddDays(days);

        var bills = await _db.Bills
            .Where(b => b.UserId == userId.Value
                     && b.Status != "paid"
                     && b.DueDate.Date >= today
                     && b.DueDate.Date <= cutoff)
            .OrderBy(b => b.DueDate)
            .ToListAsync();

        return Ok(new { success = true, data = bills.Select(BillService.MapToResponse) });
    }

    /// <summary>All overdue bills sorted by most overdue first</summary>
    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdue()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var today = DateTime.UtcNow.Date;

        var bills = await _db.Bills
            .Where(b => b.UserId == userId.Value && b.Status != "paid" && b.DueDate.Date < today)
            .OrderBy(b => b.DueDate) // oldest first = most overdue
            .ToListAsync();

        var mapped = bills.Select(BillService.MapToResponse).ToList();
        var totalOverdue = mapped.Sum(b => b.Amount);

        return Ok(new { success = true, data = mapped, totalOverdueAmount = totalOverdue });
    }

    /// <summary>Category-wise breakdown for a specific month</summary>
    [HttpGet("monthly-summary")]
    public async Task<IActionResult> GetMonthlySummary([FromQuery] int? month, [FromQuery] int? year)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var today = DateTime.UtcNow;
        var targetMonth = month ?? today.Month;
        var targetYear = year ?? today.Year;

        if (targetMonth < 1 || targetMonth > 12)
            return BadRequest(new { success = false, message = "Month must be between 1 and 12." });

        var monthStart = new DateTime(targetYear, targetMonth, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var bills = await _db.Bills
            .Where(b => b.UserId == userId.Value
                     && b.DueDate.Date >= monthStart
                     && b.DueDate.Date <= monthEnd)
            .ToListAsync();

        var total = bills.Sum(b => b.Amount);
        var paid = bills.Where(b => b.Status == "paid").Sum(b => b.Amount);
        var pending = total - paid;

        var categoryBreakdown = bills
            .GroupBy(b => b.Category)
            .Select(g => new CategoryBreakdown
            {
                Category = g.Key,
                Amount = g.Sum(b => b.Amount),
                Count = g.Count(),
                Percentage = total > 0 ? Math.Round((double)(g.Sum(b => b.Amount) / total) * 100, 1) : 0
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        return Ok(new
        {
            success = true,
            data = new
            {
                month = targetMonth,
                year = targetYear,
                totalAmount = total,
                paidAmount = paid,
                pendingAmount = pending,
                paymentPercentage = total > 0 ? Math.Round((double)(paid / total) * 100, 1) : 0,
                categoryBreakdown
            }
        });
    }

    /// <summary>Calendar view: bills grouped by date for a given month</summary>
    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendar([FromQuery] int? month, [FromQuery] int? year)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var today = DateTime.UtcNow;
        var targetMonth = month ?? today.Month;
        var targetYear = year ?? today.Year;

        if (targetMonth < 1 || targetMonth > 12)
            return BadRequest(new { success = false, message = "Month must be between 1 and 12." });

        var monthStart = new DateTime(targetYear, targetMonth, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var bills = await _db.Bills
            .Where(b => b.UserId == userId.Value
                     && b.DueDate.Date >= monthStart
                     && b.DueDate.Date <= monthEnd)
            .OrderBy(b => b.DueDate)
            .ToListAsync();

        var grouped = bills
            .GroupBy(b => b.DueDate.ToString("yyyy-MM-dd"))
            .ToDictionary(
                g => g.Key,
                g => g.Select(BillService.MapToResponse).ToList());

        return Ok(new { success = true, data = grouped });
    }

    private int? GetUserId() => _jwtService.GetUserIdFromContext(HttpContext);
}
