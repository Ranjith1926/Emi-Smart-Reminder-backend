namespace EMI_REMAINDER.DTOs.User;

public class UpdateProfileRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
}

public class UpdatePreferencesRequest
{
    public bool? PushEnabled { get; set; }
    public bool? SmsEnabled { get; set; }
    public bool? WhatsAppEnabled { get; set; }
    public string? ReminderDays { get; set; }
    public string? Language { get; set; }
}
