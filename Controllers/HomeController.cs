using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeddingApp.Data;
using WeddingApp.Models;
using WeddingApp.Models.ViewModels;

namespace WeddingApp.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var guests = await _db.Guests.ToListAsync();
        var confirmed = guests.Where(g => g.Status == RsvpStatus.Confirmed).ToList();

        var vm = new DashboardViewModel
        {
            TotalGuests = guests.Count,
            Confirmed = confirmed.Count,
            Declined = guests.Count(g => g.Status == RsvpStatus.Declined),
            Pending = guests.Count(g => g.Status == RsvpStatus.Pending),
            InvitationsSent = guests.Count(g => g.InvitationSent),
            TotalAttending = confirmed.Count + confirmed.Count(g => !string.IsNullOrEmpty(g.PlusOneName)),
            RecentRsvps = guests
                .Where(g => g.RsvpAt.HasValue)
                .OrderByDescending(g => g.RsvpAt)
                .Take(8)
                .ToList(),
            ByCategory = guests
                .GroupBy(g => g.Category.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            ByMeal = confirmed
                .GroupBy(g => g.MealPreference.ToString())
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return View(vm);
    }
}
