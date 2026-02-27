namespace EMI_REMAINDER.DTOs.Payment;

public class CreateOrderRequest
{
    public string PlanType { get; set; } = string.Empty;
}

public class CreateOrderResponse
{
    public string OrderId { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string RazorpayKey { get; set; } = string.Empty;
}

public class VerifyPaymentRequest
{
    public string OrderId { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}

public class VerifyPaymentResponse
{
    public bool IsPremium { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class PaymentStatusResponse
{
    public bool IsPremium { get; set; }
    public string? PlanType { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int DaysRemaining { get; set; }
}
