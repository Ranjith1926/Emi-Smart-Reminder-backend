using EMI_REMAINDER.Data;
using EMI_REMAINDER.DTOs.Auth;
using EMI_REMAINDER.DTOs.User;
using EMI_REMAINDER.Models;
using Microsoft.EntityFrameworkCore;

namespace EMI_REMAINDER.Services;

public class UserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserProfileResponse?> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await _db.Users.Include(u => u.Preferences).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return null;

        if (request.Name is not null) user.Name = request.Name.Trim();
        if (request.Email is not null) user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLower();

        await _db.SaveChangesAsync();
        return AuthService.MapToProfileResponse(user);
    }

    public async Task<UserPreference?> UpdatePreferencesAsync(int userId, UpdatePreferencesRequest request)
    {
        var prefs = await _db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
        if (prefs is null)
        {
            prefs = new UserPreference { UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _db.UserPreferences.Add(prefs);
        }

        if (request.PushEnabled.HasValue) prefs.PushEnabled = request.PushEnabled.Value;
        if (request.SmsEnabled.HasValue) prefs.SmsEnabled = request.SmsEnabled.Value;
        if (request.WhatsAppEnabled.HasValue) prefs.WhatsAppEnabled = request.WhatsAppEnabled.Value;
        if (request.ReminderDays is not null) prefs.ReminderDays = request.ReminderDays;
        if (request.Language is not null) prefs.Language = request.Language;

        await _db.SaveChangesAsync();
        return prefs;
    }

    public async Task<bool> UpdateFcmTokenAsync(int userId, string fcmToken)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return false;
        user.FcmToken = fcmToken;
        await _db.SaveChangesAsync();
        return true;
    }
}
