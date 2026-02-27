using EMI_REMAINDER.DTOs;
using EMI_REMAINDER.DTOs.Auth;
using EMI_REMAINDER.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMI_REMAINDER.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly JwtService _jwtService;

    public AuthController(AuthService authService, JwtService jwtService)
    {
        _authService = authService;
        _jwtService = jwtService;
    }

    /// <summary>Send OTP to an Indian mobile number</summary>
    [HttpPost("send-otp")]
    [ProducesResponseType(typeof(ApiResponse<SendOtpResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
    {
        var result = await _authService.SendOtpAsync(request.Phone);
        return Ok(new { success = true, message = "OTP sent successfully.", requestId = result.RequestId });
    }

    /// <summary>Verify OTP and get JWT token</summary>
    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(ApiResponse<VerifyOtpResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        var (response, error) = await _authService.VerifyOtpAsync(request);
        if (error is not null)
            return BadRequest(ApiResponse.Fail(error));

        return Ok(new { success = true, token = response!.Token, user = response.User });
    }

    /// <summary>Refresh JWT token</summary>
    [Authorize]
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponse>), 200)]
    public async Task<IActionResult> RefreshToken()
    {
        var userId = _jwtService.GetUserIdFromContext(HttpContext);
        if (userId is null) return Unauthorized(ApiResponse.Fail("Invalid token."));

        var user = await _authService.GetUserByIdAsync(userId.Value);
        if (user is null) return Unauthorized(ApiResponse.Fail("User not found."));

        var token = _authService.RefreshToken(user);
        return Ok(new { success = true, token });
    }

    /// <summary>Get current user profile</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), 200)]
    public async Task<IActionResult> GetMe()
    {
        var userId = _jwtService.GetUserIdFromContext(HttpContext);
        if (userId is null) return Unauthorized(ApiResponse.Fail("Invalid token."));

        var profile = await _authService.GetProfileAsync(userId.Value);
        if (profile is null) return NotFound(ApiResponse.Fail("User not found."));

        return Ok(new { success = true, user = profile });
    }
}
