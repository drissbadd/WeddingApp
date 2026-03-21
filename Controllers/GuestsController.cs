using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Text;
using WeddingApp.Data;
using WeddingApp.Models;
using WeddingApp.Services;

namespace WeddingApp.Controllers;

[Authorize]
public class GuestsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly WhatsAppService _whatsApp;

    public GuestsController(ApplicationDbContext db, WhatsAppService whatsApp)
    {
        _db = db;
        _whatsApp = whatsApp;
    }

    public async Task<IActionResult> Index(string? search, string? status, string? category)
    {
        var query = _db.Guests.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(g => g.Name.Contains(search) || g.PhoneNumber.Contains(search));

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<RsvpStatus>(status, out var s))
            query = query.Where(g => g.Status == s);

        if (!string.IsNullOrEmpty(category) && Enum.TryParse<GuestCategory>(category, out var c))
            query = query.Where(g => g.Category == c);

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.Category = category;
        ViewBag.WhatsAppConfigured = _whatsApp.IsConfigured;

        return View(await query.OrderBy(g => g.Name).ToListAsync());
    }

    public IActionResult Create() => View(new Guest());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guest guest)
    {
        if (!ModelState.IsValid) return View(guest);

        guest.RsvpToken = Guid.NewGuid().ToString("N");
        guest.CreatedAt = DateTime.UtcNow;
        guest.Status = RsvpStatus.Pending;

        _db.Guests.Add(guest);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"✓ {guest.Name} a été ajouté(e) à la liste des invités !";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var guest = await _db.Guests.FindAsync(id);
        if (guest == null) return NotFound();
        return View(guest);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Guest updated)
    {
        if (id != updated.Id) return BadRequest();
        if (!ModelState.IsValid) return View(updated);

        var guest = await _db.Guests.FindAsync(id);
        if (guest == null) return NotFound();

        guest.Name = updated.Name;
        guest.PhoneNumber = updated.PhoneNumber;
        guest.Category = updated.Category;
        guest.HasPlusOne = updated.HasPlusOne;
        guest.MealPreference = updated.MealPreference;
        guest.TableNumber = updated.TableNumber;
        guest.Notes = updated.Notes;

        await _db.SaveChangesAsync();
        TempData["Success"] = $"✓ {guest.Name} mis à jour !";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var guest = await _db.Guests.FindAsync(id);
        if (guest != null)
        {
            _db.Guests.Remove(guest);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Invité supprimé.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SendInvitation(int id)
    {
        var guest = await _db.Guests.FindAsync(id);
        if (guest == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var (success, message) = await _whatsApp.SendInvitationAsync(guest, baseUrl);

        if (success)
        {
            guest.InvitationSent = true;
            guest.InvitationSentAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = message;
        }
        else
        {
            TempData["Error"] = message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SendAllPending()
    {
        var guests = await _db.Guests.Where(g => !g.InvitationSent).ToListAsync();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        int sent = 0, failed = 0;

        foreach (var guest in guests)
        {
            var (success, _) = await _whatsApp.SendInvitationAsync(guest, baseUrl);
            if (success)
            {
                guest.InvitationSent = true;
                guest.InvitationSentAt = DateTime.UtcNow;
                sent++;
            }
            else failed++;
            await Task.Delay(300); // avoid rate limiting
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Envoyé : {sent} invitation(s). Échec : {failed}.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> GetRsvpLink(int id)
    {
        var guest = await _db.Guests.FindAsync(id);
        if (guest == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Json(new { link = $"{baseUrl}/rsvp/{guest.RsvpToken}" });
    }

    public async Task<IActionResult> QrCode(int id)
    {
        var guest = await _db.Guests.FindAsync(id);
        if (guest == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var url = $"{baseUrl}/rsvp/{guest.RsvpToken}";

        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var qrBytes = qrCode.GetGraphic(10);

        return File(qrBytes, "image/png", $"invitation-{guest.Name.Replace(" ", "_")}.png");
    }

    public async Task<IActionResult> ExportCsv()
    {
        var guests = await _db.Guests.OrderBy(g => g.Name).ToListAsync();
        var sb = new StringBuilder();
        sb.AppendLine("Nom,Téléphone,Statut,Catégorie,Repas,Accompagnant,Nom Accompagnant,Table,Invitation Envoyée,RSVP Le");

        foreach (var g in guests)
        {
            sb.AppendLine($"\"{g.Name}\",\"{g.PhoneNumber}\",{g.Status},{g.Category},{g.MealPreference},{g.HasPlusOne},\"{g.PlusOneName}\",{g.TableNumber},{g.InvitationSent},{g.RsvpAt:dd/MM/yyyy HH:mm}");
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"invites-mariage-{DateTime.Now:yyyy-MM-dd}.csv");
    }
}
