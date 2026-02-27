using EMI_REMAINDER.DTOs.Bills;

namespace EMI_REMAINDER.DTOs.Reminders;

public class ReminderResponse
{
    public int Id { get; set; }
    public int BillId { get; set; }
    public int UserId { get; set; }
    public DateTime ReminderDate { get; set; }
    public int DaysBefore { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public BillResponse? Bill { get; set; }
}

public class ReminderQueryParams
{
    public string? Status { get; set; }
    public int? BillId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class RescheduleReminderRequest
{
    public DateTime ReminderDate { get; set; }
}

public class SendTestReminderRequest
{
    public int BillId { get; set; }
    public string Channel { get; set; } = "push";
}
