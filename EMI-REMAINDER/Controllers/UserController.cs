using EMI_REMAINDER.DTOs;
using EMI_REMAINDER.DTOs.Auth;
using EMI_REMAINDER.DTOs.User;
using EMI_REMAINDER.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMI_REMAINDER.Controllers;

[ApiController]
[Route("api/user")]
[Authorize]
[Produces("application/json")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    private readonly JwtService _jwtService;

    public UserController(UserService userService, JwtService jwtService)
    {
        _userService = userService;
        _jwtService = jwtService;
    }

    /// <summary>Update user profile (name, email)</summary>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        if (request.Name is null && request.Email is null)
            return BadRequest(ApiResponse.Fail("No fields to update."));

        var profile = await _userService.UpdateProfileAsync(userId.Value, request);
        if (profile is null) return NotFound(ApiResponse.Fail("User not found."));

        return Ok(ApiResponse<UserProfileResponse>.Ok(profile));
    }

    /// <summary>Update notification preferences</summary>
    [HttpPut("preferences")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var prefs = await _userService.UpdatePreferencesAsync(userId.Value, request);
        if (prefs is null) return NotFound(ApiResponse.Fail("User not found."));

        return Ok(new
        {
            success = true,
            data = new
            {
                prefs.PushEnabled,
                prefs.SmsEnabled,
                prefs.WhatsAppEnabled,
                prefs.ReminderDays,
                prefs.Language
            }
        });
    }

    private int? GetUserId() => _jwtService.GetUserIdFromContext(HttpContext);
}
