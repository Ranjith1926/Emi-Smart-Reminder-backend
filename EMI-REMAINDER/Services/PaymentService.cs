using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EMI_REMAINDER.Data;
using EMI_REMAINDER.DTOs.Payment;
using EMI_REMAINDER.Models;
using Microsoft.EntityFrameworkCore;

namespace EMI_REMAINDER.Services;

public class PaymentService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    // Plan amounts in paise (₹99 = 9900, ₹799 = 79900)
    private static readonly Dictionary<string, int> PlanAmounts = new(StringComparer.OrdinalIgnoreCase)
    {
        { "monthly", 9900 },
        { "yearly", 79900 }
    };

    public PaymentService(AppDbContext db, IConfiguration config, ILogger<PaymentService> logger, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _config = config;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<(CreateOrderResponse? Response, string? Error)> CreateOrderAsync(int userId, string planType)
    {
        if (!PlanAmounts.TryGetValue(planType, out var amount))
            return (null, "Invalid plan type. Use 'monthly' or 'yearly'.");

        var keyId = _config["Razorpay:KeyId"]!;
        var keySecret = _config["Razorpay:KeySecret"]!;
        var orderId = string.Empty;

        // In dev mode, generate a fake order ID
        if (keyId == "dev_skip")
        {
            orderId = $"order_{Guid.NewGuid():N}";
        }
        else
        {
            // Call Razorpay API to create order
            try
            {
                var client = _httpClientFactory.CreateClient();
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{keyId}:{keySecret}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                var payload = JsonSerializer.Serialize(new
                {
                    amount,
                    currency = "INR",
                    receipt = $"receipt_{userId}_{DateTime.UtcNow.Ticks}",
                    notes = new { userId = userId.ToString(), planType }
                });

                var response = await client.PostAsync(
                    "https://api.razorpay.com/v1/orders",
                    new StringContent(payload, Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                    return (null, "Failed to create Razorpay order. Please try again.");

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                orderId = doc.RootElement.GetProperty("id").GetString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Razorpay order creation failed for user {UserId}", userId);
                return (null, "Payment service unavailable. Please try again later.");
            }
        }

        // Save to DB
        _db.Payments.Add(new Payment
        {
            UserId = userId,
            OrderId = orderId,
            Amount = amount / 100m,
            Currency = "INR",
            Status = "created",
            PlanType = planType.ToLower(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return (new CreateOrderResponse
        {
            OrderId = orderId,
            Amount = amount,
            Currency = "INR",
            RazorpayKey = keyId
        }, null);
    }

    public async Task<(VerifyPaymentResponse? Response, string? Error)> VerifyPaymentAsync(
        int userId, VerifyPaymentRequest request)
    {
        var keySecret = _config["Razorpay:KeySecret"]!;

        // Verify Razorpay signature: HMAC-SHA256(orderId + "|" + paymentId, keySecret)
        if (keySecret != "dev_skip")
        {
            var payload = $"{request.OrderId}|{request.PaymentId}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(keySecret));
            var computedHash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLower();

            if (computedHash != request.Signature)
                return (null, "Payment signature verification failed.");
        }

        var payment = await _db.Payments.FirstOrDefaultAsync(
            p => p.OrderId == request.OrderId && p.UserId == userId);

        if (payment is null)
            return (null, "Order not found.");

        payment.Status = "paid";
        payment.TransactionId = request.PaymentId;
        payment.UpdatedAt = DateTime.UtcNow;

        // Activate premium
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            return (null, "User account not found.");

        user.IsPremium = true;
        user.PremiumExpiresAt = payment.PlanType == "yearly"
            ? DateTime.UtcNow.AddYears(1)
            : DateTime.UtcNow.AddMonths(1);

        await _db.SaveChangesAsync();

        _logger.LogInformation("Premium activated for user {UserId}. Plan: {Plan}", userId, payment.PlanType);

        return (new VerifyPaymentResponse
        {
            IsPremium = true,
            ExpiresAt = user.PremiumExpiresAt.Value
        }, null);
    }

    public async Task<PaymentStatusResponse> GetStatusAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return new PaymentStatusResponse();

        var latestPayment = await _db.Payments
            .Where(p => p.UserId == userId && p.Status == "paid")
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        // Auto-expire premium before computing status
        if (user.IsPremium && user.PremiumExpiresAt < DateTime.UtcNow)
        {
            user.IsPremium = false;
            user.PremiumExpiresAt = null;
            await _db.SaveChangesAsync();
        }

        var daysRemaining = user.IsPremium && user.PremiumExpiresAt.HasValue
            ? Math.Max(0, (int)(user.PremiumExpiresAt.Value - DateTime.UtcNow).TotalDays)
            : 0;

        return new PaymentStatusResponse
        {
            IsPremium = user.IsPremium,
            PlanType = latestPayment?.PlanType,
            ExpiresAt = user.PremiumExpiresAt,
            DaysRemaining = daysRemaining
        };
    }
}
