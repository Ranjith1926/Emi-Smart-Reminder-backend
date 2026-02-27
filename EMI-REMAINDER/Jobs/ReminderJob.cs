using EMI_REMAINDER.Data;
using EMI_REMAINDER.Services;
using Microsoft.EntityFrameworkCore;

namespace EMI_REMAINDER.Jobs;

public class ReminderJob
{
    private readonly AppDbContext _db;
    private readonly SmsService _smsService;
    private readonly ILogger<ReminderJob> _logger;

    public ReminderJob(AppDbContext db, SmsService smsService, ILogger<ReminderJob> logger)
    {
        _db = db;
        _smsService = smsService;
        _logger = logger;
    }

    public async Task ProcessPendingRemindersAsync()
    {
        var now = DateTime.UtcNow;

        var dueReminders = await _db.Reminders
            .Include(r => r.Bill)
            .Include(r => r.User)
            .Where(r => r.Status == "pending" && r.ReminderDate <= now)
            .ToListAsync();

        _logger.LogInformation("Processing {Count} pending reminders.", dueReminders.Count);

        foreach (var reminder in dueReminders)
        {
            try
            {
                var sent = await SendReminderAsync(reminder.User.Phone, reminder.Message, reminder.Channel);

                reminder.Status = sent ? "sent" : "failed";
                reminder.SentAt = sent ? DateTime.UtcNow : null;

                _logger.LogInformation(
                    "Reminder {Id} for bill '{Title}' — {Status}",
                    reminder.Id, reminder.Bill.Title, reminder.Status);
            }
            catch (Exception ex)
            {
                reminder.Status = "failed";
                _logger.LogError(ex, "Failed to send reminder {Id}", reminder.Id);
            }
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist reminder statuses. {Count} reminders may resend on next run.", dueReminders.Count);
        }
    }

    private async Task<bool> SendReminderAsync(string phone, string message, string channel)
    {
        return channel switch
        {
            "sms"       => await _smsService.SendSmsAsync(phone, message),
            "whatsapp"  => await _smsService.SendWhatsAppAsync(phone, message),
            _           => LogPushNotification(phone, message) // push — log for now, integrate FCM separately
        };
    }

    private bool LogPushNotification(string phone, string message)
    {
        _logger.LogInformation("[PUSH NOTIFICATION] To: {Phone} | {Message}", phone, message);
        return true;
    }
}
