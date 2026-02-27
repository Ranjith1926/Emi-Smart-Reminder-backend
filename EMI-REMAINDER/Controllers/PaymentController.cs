using EMI_REMAINDER.DTOs;
using EMI_REMAINDER.DTOs.Payment;
using EMI_REMAINDER.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMI_REMAINDER.Controllers;

[ApiController]
[Route("api/payment")]
[Authorize]
[Produces("application/json")]
public class PaymentController : ControllerBase
{
    private readonly PaymentService _paymentService;
    private readonly JwtService _jwtService;

    public PaymentController(PaymentService paymentService, JwtService jwtService)
    {
        _paymentService = paymentService;
        _jwtService = jwtService;
    }

    /// <summary>Create a Razorpay order for premium subscription</summary>
    [HttpPost("create-order")]
    [ProducesResponseType(typeof(ApiResponse<CreateOrderResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PlanType))
            return BadRequest(ApiResponse.Fail("Plan type is required."));

        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var (response, error) = await _paymentService.CreateOrderAsync(userId.Value, request.PlanType);
        if (error is not null) return BadRequest(ApiResponse.Fail(error));

        return Ok(ApiResponse<CreateOrderResponse>.Ok(response!));
    }

    /// <summary>Verify Razorpay payment and activate premium</summary>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(ApiResponse<VerifyPaymentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId)
            || string.IsNullOrWhiteSpace(request.PaymentId)
            || string.IsNullOrWhiteSpace(request.Signature))
            return BadRequest(ApiResponse.Fail("orderId, paymentId, and signature are all required."));

        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var (response, error) = await _paymentService.VerifyPaymentAsync(userId.Value, request);
        if (error is not null) return BadRequest(ApiResponse.Fail(error));

        return Ok(new
        {
            success = true,
            message = "Premium activated!",
            data = new { response!.IsPremium, expiresAt = response.ExpiresAt }
        });
    }

    /// <summary>Get current premium subscription status</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<PaymentStatusResponse>), 200)]
    public async Task<IActionResult> GetStatus()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var status = await _paymentService.GetStatusAsync(userId.Value);
        return Ok(ApiResponse<PaymentStatusResponse>.Ok(status));
    }

    private int? GetUserId() => _jwtService.GetUserIdFromContext(HttpContext);
}
