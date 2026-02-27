using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EMI_REMAINDER.Models;
using Microsoft.IdentityModel.Tokens;

namespace EMI_REMAINDER.Services;

public class JwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(User user)
    {
        var jwtSection = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("userId", user.Id.ToString()),
            new Claim("phone", user.Phone),
            new Claim(ClaimTypes.Role, user.IsPremium ? "Premium" : "Free"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var expiryDays = int.TryParse(jwtSection["ExpiryDays"], out var days) ? days : 30;

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expiryDays),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public int? GetUserIdFromContext(HttpContext context)
    {
        //var claim = context.User.FindFirst("userId")
        //          ?? context.User.FindFirst(ClaimTypes.NameIdentifier);
        //return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;

        return int.TryParse(
       context.User.FindFirstValue(ClaimTypes.NameIdentifier),
       out var userId
   )
   ? userId
   : null;
    }
}
