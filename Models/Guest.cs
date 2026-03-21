using System.ComponentModel.DataAnnotations;

namespace WeddingApp.Models;

public enum RsvpStatus { Pending, Confirmed, Declined }
public enum GuestCategory
{
    [Display(Name = "Zineb & Driss")] ZinebDriss,
    Ouazzani,
    Berrada
}
public enum MealPreference { Halal, Standard, Vegetarian, Vegan }

public class Guest
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    [Display(Name = "Full Name")]
    public string Name { get; set; } = "";

    [Required, MaxLength(25)]
    [Display(Name = "WhatsApp Number (with country code, e.g. +212612345678)")]
    public string PhoneNumber { get; set; } = "";

    public string RsvpToken { get; set; } = Guid.NewGuid().ToString("N");

    [Display(Name = "RSVP Status")]
    public RsvpStatus Status { get; set; } = RsvpStatus.Pending;

    [Display(Name = "Category")]
    public GuestCategory Category { get; set; } = GuestCategory.Friends;

    [Display(Name = "Allow +1 Guest")]
    public bool HasPlusOne { get; set; }

    [Display(Name = "+1 Guest Name")]
    public string? PlusOneName { get; set; }

    [Display(Name = "Meal Preference")]
    public MealPreference MealPreference { get; set; } = MealPreference.Halal;

    [Display(Name = "Table #")]
    [Range(1, 200)]
    public int? TableNumber { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Display(Name = "Invitation Sent")]
    public bool InvitationSent { get; set; }

    public DateTime? InvitationSentAt { get; set; }
    public DateTime? RsvpAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
