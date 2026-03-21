using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace WeddingApp.Controllers;

public class AuthController : Controller
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config) => _config = config;

    [HttpGet("/auth/login")]
    public IActionResult Login() => View();

    [HttpPost("/auth/login")]
    [ValidateAntiForgeryToken]
    public IActionResult Login(string username, string password)
    {
        var validUser = _config["Auth:Username"];
        var validPass = _config["Auth:Password"];

        if (username != validUser || password != validPass)
        {
            ViewBag.Error = "Identifiant ou mot de passe incorrect.";
            return View();
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            claims: new[] { new Claim(ClaimTypes.Name, username) },
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        Response.Cookies.Append("jwt", jwt, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(8)
        });

        return Redirect("/");
    }

    [HttpPost("/auth/logout")]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("jwt");
        return Redirect("/auth/login");
    }
}
