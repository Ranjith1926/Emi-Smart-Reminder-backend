using EMI_REMAINDER.Data;
using EMI_REMAINDER.DTOs.Reminders;
using EMI_REMAINDER.Models;
using Microsoft.EntityFrameworkCore;

namespace EMI_REMAINDER.Services;

public class ReminderService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ReminderService> _logger;

    // IST = UTC+5:30; cross-platform timezone ID support
    private static readonly TimeZoneInfo _istZone = GetIstZone();
    private static TimeZoneInfo GetIstZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
    }

    public ReminderService(AppDbContext db, ILogger<ReminderService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task CreateRemindersForBillAsync(Bill bill, int userId)
    {
        var user = await _db.Users.Include(u => u.Preferences).FirstOrDefaultAsync(u => u.Id == userId);
        var reminderDaysStr = user?.Preferences?.ReminderDays ?? "7,3,0";
        var reminderDays = reminderDaysStr.Split(',').Select(d => int.TryParse(d.Trim(), out var v) ? v : -1)
                                          .Where(d => d >= 0).Distinct().ToList();

        var reminders = new List<Reminder>();
        var dueDate = bill.DueDate.Date;

        foreach (var daysBefore in reminderDays)
        {
            var reminderDate = dueDate.AddDays(-daysBefore);
            if (reminderDate.Date < DateTime.UtcNow.Date) continue;

            var channel = GetPreferredChannel(user?.Preferences);
            var message = BuildReminderMessage(bill, daysBefore, channel);

            // Convert 9 AM IST to UTC for storage
            var nineAmIst = new DateTime(reminderDate.Year, reminderDate.Month, reminderDate.Day,
                                         9, 0, 0, DateTimeKind.Unspecified);
            var nineAmUtc = TimeZoneInfo.ConvertTimeToUtc(nineAmIst, _istZone);

            reminders.Add(new Reminder
            {
                BillId = bill.Id,
                UserId = userId,
                ReminderDate = nineAmUtc, // 9 AM IST = 3:30 AM UTC
                DaysBefore = daysBefore,
                Message = message,
                Channel = channel,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            });
        }

        _db.Reminders.AddRange(reminders);
        await _db.SaveChangesAsync();
    }

    public async Task RescheduleRemindersForBillAsync(Bill bill, int userId)
    {
        // Cancel existing pending reminders
        await CancelPendingRemindersForBillAsync(bill.Id);
        // Create fresh ones
        await CreateRemindersForBillAsync(bill, userId);
    }

    public async Task CancelPendingRemindersForBillAsync(int billId)
    {
        var pending = await _db.Reminders
            .Where(r => r.BillId == billId && r.Status == "pending")
            .ToListAsync();
        _db.Reminders.RemoveRange(pending);
        await _db.SaveChangesAsync();
    }

    public async Task<List<ReminderResponse>> GetPendingDueRemindersAsync()
    {
        var now = DateTime.UtcNow;
        var due = await _db.Reminders
            .Include(r => r.Bill)
            .Where(r => r.Status == "pending" && r.ReminderDate <= now)
            .ToListAsync();
        return due.Select(MapToResponse).ToList();
    }

    public async Task MarkReminderSentAsync(int reminderId)
    {
        var reminder = await _db.Reminders.FindAsync(reminderId);
        if (reminder is null) return;
        reminder.Status = "sent";
        reminder.SentAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task MarkReminderFailedAsync(int reminderId)
    {
        var reminder = await _db.Reminders.FindAsync(reminderId);
        if (reminder is null) return;
        reminder.Status = "failed";
        await _db.SaveChangesAsync();
    }

    public async Task<(List<ReminderResponse> Items, int TotalCount)> GetRemindersAsync(
        int userId, string? status, int? billId, int page, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var q = _db.Reminders.Include(r => r.Bill).Where(r => r.UserId == userId).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(r => r.Status == status.ToLower());
        if (billId.HasValue)
            q = q.Where(r => r.BillId == billId.Value);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(r => r.ReminderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items.Select(MapToResponse).ToList(), total);
    }

    public async Task<ReminderResponse?> RescheduleReminderAsync(int id, int userId, DateTime newDate)
    {
        var reminder = await _db.Reminders.Include(r => r.Bill)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (reminder is null) return null;

        reminder.ReminderDate = newDate;
        reminder.Status = "pending";
        await _db.SaveChangesAsync();

        return MapToResponse(reminder);
    }

    public async Task<bool> DeleteReminderAsync(int id, int userId)
    {
        var reminder = await _db.Reminders.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (reminder is null) return false;
        _db.Reminders.Remove(reminder);
        await _db.SaveChangesAsync();
        return true;
    }

    public static string BuildReminderMessage(Bill bill, int daysBefore, string channel)
    {
        var daysText = daysBefore == 0 ? "today" : $"in {daysBefore} day{(daysBefore > 1 ? "s" : "")}";
        return channel switch
        {
            "whatsapp" => $"🔔 *EMI Reminder*\nHi! Your *{bill.Title}* payment of *₹{bill.Amount:N0}* is due {daysText} ({bill.DueDate:dd MMM yyyy}).\n\nPay on time to avoid penalties! 💰",
            "sms"      => $"EMI Reminder: {bill.Title} - Rs.{bill.Amount:N0} due {daysText} ({bill.DueDate:dd MMM yyyy}). -EmiReminder",
            _          => $"Your {bill.Title} payment of ₹{bill.Amount:N0} is due {daysText}."
        };
    }

    private static string GetPreferredChannel(UserPreference? prefs)
    {
        if (prefs is null) return "push";
        if (prefs.WhatsAppEnabled) return "whatsapp";
        if (prefs.SmsEnabled) return "sms";
        return "push";
    }

    public static ReminderResponse MapToResponse(Reminder r)
    {
        return new ReminderResponse
        {
            Id = r.Id,
            BillId = r.BillId,
            UserId = r.UserId,
            ReminderDate = r.ReminderDate,
            DaysBefore = r.DaysBefore,
            Message = r.Message,
            Channel = r.Channel,
            Status = r.Status,
            SentAt = r.SentAt,
            CreatedAt = r.CreatedAt,
            Bill = r.Bill is not null ? BillService.MapToResponse(r.Bill) : null
        };
    }
}
