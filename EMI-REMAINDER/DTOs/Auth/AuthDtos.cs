namespace EMI_REMAINDER.DTOs.Auth;

public class SendOtpRequest
{
    public string Phone { get; set; } = string.Empty;
}

public class VerifyOtpRequest
{
    public string Phone { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
}

public class SendOtpResponse
{
    public string RequestId { get; set; } = string.Empty;
    public string? DevOtp { get; set; }  // Only populated in Development mode
}

public class VerifyOtpResponse
{
    public string Token { get; set; } = string.Empty;
    public AuthUserDto User { get; set; } = null!;
}

public class AuthUserDto
{
    public int Id { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsNewUser { get; set; }
}

public class RefreshTokenResponse
{
    public string Token { get; set; } = string.Empty;
}

public class UserProfileResponse
{
    public int Id { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsPremium { get; set; }
    public DateTime? PremiumExpiresAt { get; set; }
    public UserPreferenceDto? Preferences { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserPreferenceDto
{
    public bool PushEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public bool WhatsAppEnabled { get; set; }
    public string ReminderDays { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
}
