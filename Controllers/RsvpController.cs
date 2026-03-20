using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeddingApp.Data;
using WeddingApp.Models;

namespace WeddingApp.Controllers;

public class RsvpController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public RsvpController(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [Route("rsvp/{token}")]
    public async Task<IActionResult> Index(string token)
    {
        var guest = await _db.Guests.FirstOrDefaultAsync(g => g.RsvpToken == token);
        if (guest == null) return NotFound();

        ViewBag.WeddingDate = _config["Wedding:Date"] ?? "2025-06-21";
        ViewBag.Venue = _config["Wedding:Venue"] ?? "";
        return View(guest);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Route("rsvp/{token}/confirm")]
    public async Task<IActionResult> Confirm(string token, string? plusOneName, MealPreference mealPreference)
    {
        var guest = await _db.Guests.FirstOrDefaultAsync(g => g.RsvpToken == token);
        if (guest == null) return NotFound();

        guest.Status = RsvpStatus.Confirmed;
        guest.RsvpAt = DateTime.UtcNow;
        guest.MealPreference = mealPreference;

        if (guest.HasPlusOne && !string.IsNullOrWhiteSpace(plusOneName))
            guest.PlusOneName = plusOneName;

        await _db.SaveChangesAsync();
        ViewBag.GuestName = guest.Name;
        return View("Confirmed");
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Route("rsvp/{token}/decline")]
    public async Task<IActionResult> Decline(string token)
    {
        var guest = await _db.Guests.FirstOrDefaultAsync(g => g.RsvpToken == token);
        if (guest == null) return NotFound();

        guest.Status = RsvpStatus.Declined;
        guest.RsvpAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        ViewBag.GuestName = guest.Name;
        return View("Declined");
    }
}
