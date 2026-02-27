using System.Security.Cryptography;
using EMI_REMAINDER.Data;
using EMI_REMAINDER.DTOs.Auth;
using EMI_REMAINDER.Models;
using Microsoft.EntityFrameworkCore;

namespace EMI_REMAINDER.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwtService;
    private readonly SmsService _smsService;
    private readonly ILogger<AuthService> _logger;
    private readonly bool _isDevelopment;

    public AuthService(AppDbContext db, JwtService jwtService, SmsService smsService, ILogger<AuthService> logger, IWebHostEnvironment env)
    {
        _db = db;
        _jwtService = jwtService;
        _smsService = smsService;
        _logger = logger;
        _isDevelopment = env.IsDevelopment();
    }

    public async Task<SendOtpResponse> SendOtpAsync(string phone)
    {
        // Generate 6-digit OTP
        var otp = GenerateOtp();
        var otpHash = BCrypt.Net.BCrypt.HashPassword(otp);
        var requestId = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.AddMinutes(10);

        // Remove previous OTPs for this phone
        var old = await _db.OtpRecords.Where(o => o.Phone == phone).ToListAsync();
        _db.OtpRecords.RemoveRange(old);

        _db.OtpRecords.Add(new OtpRecord
        {
            Phone = phone,
            OtpHash = otpHash,
            RequestId = requestId,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var message = $"Your EMI Reminder OTP is {otp}. Valid for 10 minutes. Do not share this code.";
        await _smsService.SendSmsAsync(phone, message);

        _logger.LogInformation("OTP for {Phone} is {Otp}. RequestId: {RequestId}", phone, otp, requestId);
        return new SendOtpResponse
        {
            RequestId = requestId,
            DevOtp = _isDevelopment ? otp : null
        };
    }

    public async Task<(VerifyOtpResponse? Response, string? Error)> VerifyOtpAsync(VerifyOtpRequest request)
    {
        var record = await _db.OtpRecords
            .FirstOrDefaultAsync(o => o.Phone == request.Phone && o.RequestId == request.RequestId);

        if (record is null)
            return (null, "Invalid OTP request. Please request a new OTP.");

        if (record.ExpiresAt < DateTime.UtcNow)
        {
            _db.OtpRecords.Remove(record);
            await _db.SaveChangesAsync();
            return (null, "OTP has expired. Please request a new one.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Otp, record.OtpHash))
        {
            record.FailedAttempts++;
            if (record.FailedAttempts >= 5)
            {
                _db.OtpRecords.Remove(record);
                await _db.SaveChangesAsync();
                return (null, "Too many failed attempts. Please request a new OTP.");
            }
            await _db.SaveChangesAsync();
            return (null, "Invalid OTP. Please try again.");
        }

        // Find or create user
        var isNewUser = false;
        var user = await _db.Users.Include(u => u.Preferences).FirstOrDefaultAsync(u => u.Phone == request.Phone);
        if (user is null)
        {
            isNewUser = true;
            user = new User
            {
                Phone = request.Phone,
                Name = "User",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Create default preferences
            _db.UserPreferences.Add(new UserPreference
            {
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        // Delete all OTP records for this phone
        var allOtps = await _db.OtpRecords.Where(o => o.Phone == request.Phone).ToListAsync();
        _db.OtpRecords.RemoveRange(allOtps);
        await _db.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        return (new VerifyOtpResponse
        {
            Token = token,
            User = new AuthUserDto
            {
                Id = user.Id,
                Phone = user.Phone,
                Name = user.Name,
                IsNewUser = isNewUser
            }
        }, null);
    }

    public async Task<UserProfileResponse?> GetProfileAsync(int userId)
    {
        var user = await _db.Users
            .Include(u => u.Preferences)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return null;

        return MapToProfileResponse(user);
    }

    public string RefreshToken(User user) => _jwtService.GenerateToken(user);

    public async Task<User?> GetUserByIdAsync(int userId)
        => await _db.Users.Include(u => u.Preferences).FirstOrDefaultAsync(u => u.Id == userId);

    public static UserProfileResponse MapToProfileResponse(User user)
    {
        return new UserProfileResponse
        {
            Id = user.Id,
            Phone = user.Phone,
            Name = user.Name,
            Email = user.Email,
            IsPremium = user.IsPremium,
            PremiumExpiresAt = user.PremiumExpiresAt,
            CreatedAt = user.CreatedAt,
            Preferences = user.Preferences is null ? null : new UserPreferenceDto
            {
                PushEnabled = user.Preferences.PushEnabled,
                SmsEnabled = user.Preferences.SmsEnabled,
                WhatsAppEnabled = user.Preferences.WhatsAppEnabled,
                ReminderDays = user.Preferences.ReminderDays,
                Language = user.Preferences.Language
            }
        };
    }

    private static string GenerateOtp()
    {
        Span<byte> buffer = stackalloc byte[4];
        RandomNumberGenerator.Fill(buffer);
        var value = Math.Abs(BitConverter.ToInt32(buffer)) % 1_000_000;
        return value.ToString("D6");
    }
}
