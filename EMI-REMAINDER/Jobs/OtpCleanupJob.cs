using EMI_REMAINDER.Data;
using Microsoft.EntityFrameworkCore;

namespace EMI_REMAINDER.Jobs;

public class OtpCleanupJob
{
    private readonly AppDbContext _db;
    private readonly ILogger<OtpCleanupJob> _logger;

    public OtpCleanupJob(AppDbContext db, ILogger<OtpCleanupJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task CleanupExpiredOtpsAsync()
    {
        var expired = await _db.OtpRecords
            .Where(o => o.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        if (expired.Count == 0)
        {
            _logger.LogDebug("OTP cleanup: no expired records found.");
            return;
        }

        _db.OtpRecords.RemoveRange(expired);
        await _db.SaveChangesAsync();

        _logger.LogInformation("OTP cleanup: removed {Count} expired records.", expired.Count);
    }
}
